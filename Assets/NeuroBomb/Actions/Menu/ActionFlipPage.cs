using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using NeuroSdk.Messages.Outgoing;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ActionFlipPage : NeuroAction<Selectable>{

    private readonly MenuManager manager;
	private readonly MissionTableOfContentsPage page;

    public ActionFlipPage(MenuManager menuManager, MissionTableOfContentsPage tocPage)
    {
		page = tocPage;
        manager = menuManager;
    }

	public override string Name{
		get { return "flip_page"; }}
    protected override string Description{
		get { return "View another page of missions."; }}
	protected override JsonSchema Schema{
		get{
			List<string> pages = new List<string>();

			if(page.NextButton.activeSelf){
				pages.Add("next");}

			if(page.PreviousButton.activeSelf){
				pages.Add("previous");}

			return new JsonSchema{
				Type = JsonSchemaType.Object,
				Required = new List<string> { "page" },
				Properties = new Dictionary<string, JsonSchema>{
					{
						"page",
						new JsonSchema
						{
							Type = JsonSchemaType.String,
							Enum = pages.Cast<object>().ToList()
						}
					}
				}
			};
		}
	}
    protected override ExecutionResult Validate(ActionJData actionData, out Selectable selectable)
    {
		string direction = actionData.Data["page"].Value<string>();
		selectable = null;
		GameObject button;

		if (direction == "next"){
			button = page.NextButton;}
		else if (direction == "previous"){
			button = page.PreviousButton;}
		else{
			return ExecutionResult.Failure(
				string.Format("Invalid page direction: {0}.", direction));}

		if (button == null){
			return ExecutionResult.Failure(
				string.Format("The {0} button was null.", direction));}	

		if (!button.activeSelf){
			return ExecutionResult.Failure(
				string.Format("The {0} page is not available.", direction));}

		selectable = button.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure(string.Format("The {0} page button has no Selectable component.",direction));}

		return ExecutionResult.Success(
			string.Format("Flipping to the {0} page.", direction));
	}

	protected override void Execute(Selectable selectable)
	{
        manager.Select(selectable, true);
	}
}
