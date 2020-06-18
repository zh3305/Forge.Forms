using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Forge.Forms.Controls;
using Forge.Forms.FormBuilding;
using Type = System.Type;

namespace Forge.Forms
{

    public static class Show
    {
        public static IModelHostFactory DialogModelHostFactory { get; set; } = new DialogModelHostFactory();
        public static IModelHostFactory WindowModelHostFactory { get; set; } = new DialogModelHostFactory();

        public static IModelHost Window()
        {
            return WindowModelHostFactory.CreateModelHost(null,null, WindowOptions.Default);
        }

        public static IModelHost Window(object context)
        {
            return WindowModelHostFactory.CreateModelHost(null,context, WindowOptions.Default);
        }

        public static IModelHost Window(WindowOptions options)
        {
            return WindowModelHostFactory.CreateModelHost(null,null, options);
        }

        public static IModelHost Window(double width)
        {
            return WindowModelHostFactory.CreateModelHost(null,null, new WindowOptions { Width = width });
        }

        public static IModelHost Window(object context, WindowOptions options)
        {
            return WindowModelHostFactory.CreateModelHost(null,context, options);
        }

        public static IModelHost Window(object context, double width)
        {
            return WindowModelHostFactory.CreateModelHost(null,context, new WindowOptions { Width = width });
        }

        public static IModelHost Dialog()
        {
            return DialogModelHostFactory.CreateModelHost(null, null, DialogOptions.Default);
        }

        public static IModelHost Dialog(DialogOptions options)
        {
            return DialogModelHostFactory.CreateModelHost(null, null, options);
        }

        public static IModelHost Dialog(double width)
        {
            return DialogModelHostFactory.CreateModelHost(null, null, new DialogOptions { Width = width });
        }

        public static IModelHost Dialog(object dialogIdentifier)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, null, DialogOptions.Default);
        }

        public static IModelHost Dialog(object dialogIdentifier, object context)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, context, DialogOptions.Default);
        }

        public static IModelHost Dialog(object dialogIdentifier, DialogOptions options)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, null, options);
        }

        public static IModelHost Dialog(object dialogIdentifier, double width)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, null, new DialogOptions { Width = width });
        }

        public static IModelHost Dialog(object dialogIdentifier, object context, DialogOptions options)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, context, options);
        }

        public static IModelHost Dialog(object dialogIdentifier, object context, double width)
        {
            return DialogModelHostFactory.CreateModelHost(dialogIdentifier, context, new DialogOptions { Width = width });
        }

        public static bool CloseDialog(object identifer,FrameworkElement dialog)
        {
            if ( DialogModelHostFactory.CloseModelHost(identifer, dialog))
            {
                return true;
            }
            if(WindowModelHostFactory.CloseModelHost(identifer,dialog))
            {
                return true;
            }
            return false;
        }
    }

    public interface IModelHost
    {
        Task<DialogResult<T>> For<T>(T model);

        Task<DialogResult> For(Type type);

        Task<DialogResult> For(IFormDefinition formDefinition);
    }

    public interface IModelHostFactory
    {
        IModelHost CreateModelHost(object identifier,object modelContext, DialogOptions dialogOptions);
        bool CloseModelHost(object Identifier, FrameworkElement dialog);
    }

    public static class ModelHostExtensions
    {
        public static async Task<DialogResult<T>> For<T>(this IModelHost modelHost)
        {
            var result = await modelHost.For(typeof(T));
            return new DialogResult<T>((T)result.Model, result.Action, result.ActionParameter);
        }
    }

    public class DialogResult
    {
        public DialogResult(object model, object action, object actionParameter)
        {
            Model = model;
            Action = action;
            ActionParameter = actionParameter;
        }

        public object Model { get; }

        public object Action { get; }

        public object ActionParameter { get; }

        internal DialogResult<T> MakeGeneric<T>()
        {
            return new DialogResult<T>((T)Model, Action, ActionParameter);
        }
    }

    public class DialogResult<T>
    {
        public DialogResult(T model, object action, object actionParameter)
        {
            Model = model;
            Action = action;
            ActionParameter = actionParameter;
        }

        public T Model { get; }

        public object Action { get; }

        public object ActionParameter { get; }
    }


    public class DialogModelHost : IModelHost
    {
        private readonly object context;
        private readonly object dialogIdentifier;
        private readonly DialogOptions options;

        private Window window;

        public DialogModelHost(object dialogIdentifier, object context, DialogOptions options)
        {
            this.context = context;
            this.options = options;
            this.dialogIdentifier = dialogIdentifier;
        }

        public async Task<DialogResult<T>> For<T>(T model)
        {
            return (await ShowDialog(model)).MakeGeneric<T>();
        }

        public Task<DialogResult> For(Type type)
        {
            return ShowDialog(type);
        }

        public Task<DialogResult> For(IFormDefinition formDefinition)
        {
            return ShowDialog(formDefinition);
        }

        protected virtual Task<DialogResult> ShowDialog(object model)
        {
            object lastAction = null;
            object lastActionParameter = null;
            var wrapper = new DynamicFormWrapper(model, context, options);
            wrapper.Form.OnAction += (s, e) =>
            {
                lastAction = e.ActionContext.Action;
                lastActionParameter = e.ActionContext.ActionParameter;
            };

            window = new Window();
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Content = wrapper;
            window.Closed += Window_Closed;
            Window onwnerWindow = null;
            foreach(var item in Application.Current.Windows)
            {
                if(item is Window w && w.IsActive)
                {
                    onwnerWindow = w;
                }
            }
            if (onwnerWindow != null)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Owner = onwnerWindow;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            window.ShowDialog();
            var result = new DialogResult(wrapper.Form.Value, lastAction, lastActionParameter);
            return Task.FromResult(result);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            window.Closed -= Window_Closed;
            window = null;
        }

        public void Close()
        {
            window?.Close();
        }
    }

    internal class DialogModelHostFactory : IModelHostFactory
    {
        private static readonly object nullIdentifier = new object();
        private readonly Dictionary<object, DialogModelHost> _dialogs = new Dictionary<object, DialogModelHost>();
        public IModelHost CreateModelHost(object identifier, object modelContext, DialogOptions dialogOptions)
        {
            CloseByIdentifier(identifier);
            var host = new DialogModelHost(identifier, modelContext, dialogOptions);
            _dialogs.Add(identifier ?? nullIdentifier, host);
            return host;

        }

        public bool CloseModelHost(object identifier, FrameworkElement dialog)
        {

            return CloseByIdentifier(identifier);
        }

        private bool CloseByIdentifier(object identifier)
        {
            var id = identifier ?? nullIdentifier;
            if (_dialogs.TryGetValue(id, out var host))
            {
                host.Close();
                _dialogs.Remove(id);
                return true;
            }
            return false;
        }
    }
}
