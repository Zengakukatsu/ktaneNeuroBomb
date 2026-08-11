using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ActionStartMission : NeuroAction {

    private readonly MenuManager 		manager;
	private readonly MissionDetailPage 	page;
	private Selectable 		            selectable;

	public override string Name{
		get { return "start_mission"; }}
    protected override string Description{
		get { return "Starts the currently viewed Mission."; }}
    protected override JsonSchema Schema{
        get{
            return new JsonSchema{Type = JsonSchemaType.Object};
        }
    }

    public ActionStartMission(MenuManager menuManager, MissionDetailPage missionPage)
    {
        manager = menuManager;
        page = missionPage;
    }

    protected override ExecutionResult Validate(ActionJData actionData)
    {
        selectable = null;

        if (page == null)
        {
            return ExecutionResult.Failure(
                "The mission detail page was not found.");
        }

        if (page.StartButton == null)
        {
            return ExecutionResult.Failure(
                "The mission start button was not found.");
        }

        selectable = page.StartButton.GetComponent<Selectable>();
        return ExecutionResult.Success(
            "Starting the selected mission.");
    }
	
	protected override void Execute()
	{
        manager.Select(selectable, false);
	}
}
