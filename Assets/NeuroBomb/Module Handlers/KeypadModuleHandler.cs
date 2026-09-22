using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class KeypadModuleHandler : BombModuleHandler
{
    private const bool use_text_names = true;

    private static readonly Dictionary<string, string> SymbolNames =
        new Dictionary<string, string>{
            { "\u00A9", "copyright" },
            { "\u2605", "filled star" },
            { "\u2606", "hollow star" },
            { "\u067C", "smiley face" },
            { "\u0496", "I with arms and legs" },
            { "\u03A9", "omega" },
            { "\u046C", "upside down triangle with lines" },
            { "\u047C", "pumpkin" },
            { "\u03D7", "H with a flick" },
            { "\u03EB", "right C" },
            { "\u03EC", "six" },
            { "\u03DE", "lightning bolt" },
            { "\u0466", "Pyramid" },
            { "\u04D5", "A E" },
            { "\u0506", "Hook thing" },
            { "\u04EC", "euro" },
            { "\u0488", "hundred thousands sign" },
            { "\u048A", "N with hat" },
            { "\u046F", "dragon" },
            { "\u00BF", "upside-down question mark" },
            { "\u00B6", "paragraph" },
            { "\u03FE", "C with dot" },
            { "\u03FF", "backwards C with dot" },
            { "\u03A8", "pitchfork" },
            { "\u046A", "triangle with three lines" },
            { "\u04A8", "C Q" },
            { "\u0482", "half a hashtag" },
            { "\u03D8", "balloon" },
            { "\u03B6", "squiggly N" },
            { "\u019B", "lambda" },
            { "\u0463", "B with a line" }
        };

    private readonly KeypadComponent component;

    public KeypadModuleHandler(KeypadComponent component)
	{
        this.component = component;
    }

    public override string GetContext(){
        StringBuilder context = new StringBuilder("The keypad symbols are: ");

        for (int i = 0; i < component.buttons.Length; i++){
            if (i > 0) context.Append(", ");

            context.Append(GetSymbolName(component.buttons[i]));
        }

        context.Append(
            use_text_names
                ? ". Use press_key with a symbol name."
                : ". Use press_key with the symbol.");

        return context.ToString();
    }

    public override void RegisterActions(ActionWindow window, BombManager manager)
    {
        window
            .SetContext(GetContext())
            .AddAction(new ActionPressKey(manager, component))
		    .SetPersistent()
            .Register();
    }

    public static string GetSymbolName(KeypadButton button)
    {
        string symbol = button.GetValue();

        if (!use_text_names) return symbol;

        string name;

        if (!SymbolNames.TryGetValue(symbol, out name)) name = "unknown symbol";

        return name;
    }

    private class ActionPressKey : BusyAction<Selectable>
    {
        private readonly KeypadComponent component;
        private readonly Dictionary<string, KeypadButton> buttons;

        public ActionPressKey(BombManager manager, KeypadComponent component) : base(manager)
        {
            this.component = component;

            buttons = component.buttons.ToDictionary(
                button => KeypadModuleHandler.GetSymbolName(button),
                button => button);
        }

        public override string Name{
            get { return "press_key"; }}

        protected override string Description{
			get{return ConfigHelper.Get("FALLBACK - Press one of the symbols on the keypad.", "action_descriptions", Name);}}

        protected override JsonSchema Schema{
            get{
                return new JsonSchema{
                    Type = JsonSchemaType.Object,
                    Required = new List<string> { "symbol" },
                    Properties = new Dictionary<string, JsonSchema>
                        {
                            {
                                "symbol",
                                new JsonSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Enum = buttons.Keys
                                        .Select(x => (object)x)
                                        .ToList()
                                }
                            }
                        }
                };
            }
        }

        protected override ExecutionResult ValidateAction(ActionJData actionData, out Selectable selectable)
        {
            selectable = null;

            if (component.IsSolved){
                return ExecutionResult.Failure("The module is already solved. Try switching Modules");}

            if (actionData.Data == null || actionData.Data["symbol"] == null){
                return ExecutionResult.Failure("symbol was missing.");}

            string symbol = actionData.Data["symbol"].ToString();

            KeypadButton button;

            if (!buttons.TryGetValue(symbol, out button)){
                return ExecutionResult.Failure("That symbol is not on the keypad.");}

            if (button.IsStayingDown){
				return ExecutionResult.Failure("That key has already been pressed.");}

            selectable = button.GetComponent<Selectable>();

            if (selectable == null){
                return ExecutionResult.Failure("That key is not selectable.");}

            return ExecutionResult.Success("Pressing " + symbol + "...");
        }

        protected override void Execute(Selectable selectable)
        {
            manager.StartCoroutine(PressKey(selectable));
        }

        private IEnumerator PressKey(Selectable selectable)
        {
            manager.IsBusy = true;
            KeypadButton button = selectable.GetComponent<KeypadButton>();
            ResultTracker result = new ResultTracker(component);

            yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
            yield return null;

            string light = button.IsStayingDown
        		? "The key's light turned green. "
                : "The key flashed red. ";

            Context.Send(KeypadModuleHandler.GetSymbolName(button) + " pressed. " + light + result.Read());
            manager.IsBusy = false;
        }
    }
}
