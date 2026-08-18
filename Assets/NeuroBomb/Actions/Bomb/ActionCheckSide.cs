using System;
using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

public class ActionCheckSides : BusyAction<string> {

	public ActionCheckSides(BombManager bomb_manager) : base(bomb_manager)
	{
	}

	public override string Name {
		get { return "check_sides"; }}
	protected override string Description {
		get { return "Look at one side of the bomb and report the widgets found there.";}}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> { "side" },
				Properties = new Dictionary<string, JsonSchema> {{
						"side",
						new JsonSchema {
							Type = JsonSchemaType.String,
							Enum = new List<object> {
								"top", "left", "right", "bottom"
							}
						}
					}
				}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out string side)
	{
		side = null;

		if (action_data.Data == null || action_data.Data["side"] == null){
			return ExecutionResult.Failure("side was missing.");}

		side = action_data.Data["side"].ToString();

		int side_index = GetSideIndex(side);
		if (side_index < 0 || side_index > 3){
			return ExecutionResult.Failure("That side does not exist.");}

		return ExecutionResult.Success(
			"Checking the " + side + " side...");
	}

	protected override void Execute(string side)
	{
		manager.StartCoroutine(IExecute(side));
	}

	private IEnumerator IExecute(string side)
	{
		manager.IsBusy = true;

		yield return manager.StartCoroutine(BombRotationHelper.PeekSide(side));

		int side_index = GetSideIndex(side);
		List<string> widgets = new List<string>();

		foreach (Widget widget in UnityEngine.Object.FindObjectsOfType<Widget>())
		{
			Quaternion relative_rotation = Quaternion.Inverse(manager.bomb.transform.rotation) * widget.transform.rotation;
			float y = relative_rotation.eulerAngles.y;
			int widget_side = Mathf.RoundToInt(y / 90f) % 4;

			if (widget_side != side_index) continue;

			string info = GetWidgetInfo(widget);

			if (!string.IsNullOrEmpty(info)){
				widgets.Add(info);}
		}

		string widget_info = widgets.Count == 0
			? "no widgets."
			: string.Join(" ", widgets.ToArray());

		Context.Send(string.Format(
			"The {0} side has: {1}",
			side,
			widget_info));

		manager.IsBusy = false;
	}

	private static int GetSideIndex(string side)
	{
		if (side == "bottom") return 0;
		if (side == "left") return 1;
		if (side == "top") return 2;
		if (side == "right") return 3;
		return -1;
	}

	private static string GetWidgetInfo(Widget widget)
	{
		if (widget is BatteryWidget){
			int count = ((BatteryWidget)widget).GetNumberOfBatteries();

			return count == 1
				? "A battery holder containing 1 battery."
				: "A battery holder containing " +
					count + " batteries.";
		}

		if (widget is SerialNumber){
			return "Serial number: " + ((SerialNumber)widget).GetSerialString() + ".";}

		if (widget is IndicatorWidget)
		{
			IndicatorWidget indicator = (IndicatorWidget)widget;

			return "An indicator labeled " +
				indicator.Label + " which is " +
				(indicator.On ? "lit." : "unlit.");
		}

		if (widget is PortWidget)
		{
			PortWidget port_widget = (PortWidget)widget;
			List<string> ports = new List<string>();

			foreach (PortWidget.PortType port in Enum.GetValues(typeof(PortWidget.PortType))){
				if (port_widget.IsPortPresent(port)){
					ports.Add(port.ToString());}
			}

			if (ports.Count == 0){
				return "An empty port plate.";}

			return "A port plate containing " + string.Join(", ", ports.ToArray()) + ".";
		}
		return null;
	}
}