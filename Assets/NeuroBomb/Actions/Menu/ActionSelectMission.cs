
using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class ActionSelectMission : NeuroAction<Selectable>
{
    private readonly MenuManager manager;
    private readonly List<MissionTableOfContentsMissionEntry> missions;

    public ActionSelectMission(
        MenuManager menuManager,
        List<MissionTableOfContentsMissionEntry> availableMissions)
    {
        manager = menuManager;
        missions = availableMissions;
    }

    public override string Name{
        get { return "select_mission"; }}
    protected override string Description{
        get { return "Select a mission to view its details and try to start it."; }}
    protected override JsonSchema Schema{
        get{
			return new JsonSchema{
				Type = JsonSchemaType.Object,
				Required = new List<string> { "mission_name" },
				Properties = new Dictionary<string, JsonSchema>{
					{
						"mission_name",
						new JsonSchema
						{
							Type = JsonSchemaType.String,
                            Enum = missions
                                .Select(x => (object)x.EntryText.text)
                                .ToList()
						}
					}
				}
			};
        }
    }

    protected override ExecutionResult Validate(
        ActionJData actionData,
        out Selectable selectable)
    {
		MissionTableOfContentsMissionEntry entry = null;

    	selectable = null;
        string missionName = null;

        if (actionData.Data != null &&
            actionData.Data["mission_name"] != null)
		{
			missionName = actionData.Data["mission_name"].ToString();
		}

        if (string.IsNullOrEmpty(missionName))
        {
            return ExecutionResult.Failure("mission_name was null.");
        }

        foreach (MissionTableOfContentsMissionEntry mission in missions)
        {
            if (mission.EntryText.text == missionName)
            {
                entry = mission;
				selectable = entry.Selectable;

				if(selectable == null){
					return ExecutionResult.Failure(
                    	string.Format("Mission: {0}'s selectable was null, try another mission.", missionName));
				}

                return ExecutionResult.Success(
                    string.Format("Selected mission: {0}", missionName));
            }
        }
        return ExecutionResult.Failure(
            string.Format("Mission: {0} was not available.", missionName));
    }

    protected override void Execute(Selectable selectable)
    {
        manager.Select(selectable, true);
    }
}