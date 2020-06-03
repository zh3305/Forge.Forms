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
            return DialogModelHostFactory.CloseModelHost(identifer);
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
        bool CloseModelHost(object Identifier);
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

            var window = new Window();
            window.Content = wrapper;
            window.ShowDialog();
            var result = new DialogResult(wrapper.Form.Value, lastAction, lastActionParameter);
            return Task.FromResult(result);
        }
    }

    internal class DialogModelHostFactory : IModelHostFactory
    {
        public IModelHost CreateModelHost(object identifier, object modelContext, DialogOptions dialogOptions)
        {
            return new DialogModelHost(identifier, modelContext, dialogOptions);
        }

        public bool CloseModelHost(object Identifier)
        {
            //throw new System.NotImplementedException();
            return false;
        }
    }
}
