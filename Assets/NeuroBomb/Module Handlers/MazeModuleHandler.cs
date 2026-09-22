using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class MazeModuleHandler : BombModuleHandler {

	private readonly InvisibleWallsComponent component;

	public MazeModuleHandler(InvisibleWallsComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		List<string> markers = new List<string>();

		for (int y = 0; y < component.Maze.Size; y++)
		{
			for (int x = 0; x < component.Maze.Size; x++)
			{
				MazeCell cell = component.Maze.GetCell(x, y);

				if (cell.IsIdentifier)
					markers.Add(GetPosition(cell));
			}
		}

		return string.Format(
			"The circle markers are at {0}. The red triangle is at {1}. The white light is at {2}.",
			string.Join(" and ", markers.ToArray()),
			GetPosition(component.GoalCell),
			GetPosition(component.CurrentCell));
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		window
    		.SetContext(GetContext())
			.AddAction(new ActionMoveMaze(manager, component))
			.AddAction(new ActionCheckMazePositions(this))
			.SetPersistent()
			.Register();
	}

	private static string GetPosition(MazeCell cell)
	{
		return ((char)('A' + cell.Y)).ToString() + (cell.X + 1);
	}

	private class ActionCheckMazePositions : NeuroAction {

		private readonly MazeModuleHandler handler;

		public ActionCheckMazePositions(MazeModuleHandler handler)
		{
			this.handler = handler;
		}

		public override string Name{
			get { return "check_maze_positions"; }}

		protected override string Description{
			get { return ConfigHelper.Get("FALLBACK - Check the positions of the symbols on the maze.", "action_descriptions", Name); }}

		protected override JsonSchema Schema{
			get{
				return new JsonSchema{
					Type = JsonSchemaType.Object
				};
			}
		}

		protected override ExecutionResult Validate(ActionJData actionData)
		{
			return ExecutionResult.Success(handler.GetContext());
		}

		protected override void Execute()
		{
		}
	}

	private class ActionMoveMaze : BusyAction<Selectable> {

		private readonly InvisibleWallsComponent component;

		public ActionMoveMaze(
			BombManager manager,
			InvisibleWallsComponent component) : base(manager)
		{
			this.component = component;
		}

		public override string Name{
			get { return "move_maze"; }}

		protected override string Description{
			get { return ConfigHelper.Get("FALLBACK - Press one of the maze's directional arrows.", "action_descriptions", Name); }}

		protected override JsonSchema Schema{
			get{
				return new JsonSchema{
					Type = JsonSchemaType.Object,
					Required = new List<string> { "direction" },
					Properties = new Dictionary<string, JsonSchema>{
						{
							"direction",
							new JsonSchema{
								Type = JsonSchemaType.String,
								Enum = new List<object>{
									"up", "down", "left", "right"
								}
							}
						}
					}
				};
			}
		}

		protected override ExecutionResult ValidateAction(
			ActionJData actionData,
			out Selectable selectable)
		{
			selectable = null;

			if (component.IsSolved)
				return ExecutionResult.Failure("The module is already solved.");

			if (actionData.Data == null || actionData.Data["direction"] == null)
				return ExecutionResult.Failure("direction was missing.");

			string direction =actionData.Data["direction"].ToString();

			int buttonIndex = GetButtonIndex(direction);

			if (buttonIndex < 0)
				return ExecutionResult.Failure("direction must be up, down, left, or right.");

			if (IsAtEdge(direction))
				return ExecutionResult.Failure("You cannot go " + direction + " because the white light is on the edge of the maze.");

			if (component.Buttons.Count <= buttonIndex)
				return ExecutionResult.Failure("That direction is not selectable.");

			selectable = component.Buttons[buttonIndex].GetComponent<Selectable>();

			if (selectable == null)
				return ExecutionResult.Failure("That direction is not selectable.");

			return ExecutionResult.Success("Pressing " + direction + "...");
		}

		protected override void Execute(Selectable selectable)
		{
			manager.StartCoroutine(Move(selectable));
		}

		private IEnumerator Move(Selectable selectable)
		{
			manager.IsBusy = true;

			MazeCell previousCell = component.CurrentCell;
			ResultTracker result = new ResultTracker(component);
			string direction = GetDirection(selectable);

			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
			yield return null;

			string outcome = result.Read();

			if (component.CurrentCell == previousCell){
				Context.Send("You ran into a wall. You see a red line blocking the " + direction + " direction. You remained at " + GetPosition(component.CurrentCell) + ". " + outcome);}
			else{
				Context.Send("Pressed " + direction + ". The white light is at " + GetPosition(component.CurrentCell) + ". " + outcome);}

			manager.IsBusy = false;
		}

		private string GetDirection(Selectable selectable)
		{
			for (int i = 0; i < component.Buttons.Count; i++){
				if (component.Buttons[i].GetComponent<Selectable>() == selectable)
					return GetDirection(i);}

			return "unknown";
		}

		private static string GetDirection(int buttonIndex)
		{
			if (buttonIndex == 0) return "up";
			if (buttonIndex == 1) return "left";
			if (buttonIndex == 2) return "right";
			if (buttonIndex == 3) return "down";

			return "unknown";
		}

		private static int GetButtonIndex(string direction)
		{
			if (direction == "up") return 0;
			if (direction == "left") return 1;
			if (direction == "right") return 2;
			if (direction == "down") return 3;

			return -1;
		}

		private bool IsAtEdge(string direction)
		{
			if (direction == "up")
				return component.CurrentCell.Y == 0;
			if (direction == "down")
				return component.CurrentCell.Y == component.Maze.Size - 1;
			if (direction == "left")
				return component.CurrentCell.X == 0;
			if (direction == "right")
				return component.CurrentCell.X == component.Maze.Size - 1;
			return false;
		}
	}
}
