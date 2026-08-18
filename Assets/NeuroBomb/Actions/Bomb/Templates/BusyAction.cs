using NeuroSdk.Actions;
using NeuroSdk.Websocket;

// BusyAction is the base class for all actions that cannot occur while
// a visual effect (highlighting, rotation, etc) is in progress. Actions
// set IsBusy in the BombManager to true as needed. A BusyAction checks the
// bool before executing and returns a message informing Neuro to wait if Busy.

public abstract class BusyAction : NeuroAction {

	protected readonly BombManager manager;
	protected BusyAction(BombManager bombManager){manager = bombManager;}

	protected sealed override ExecutionResult Validate(ActionJData actionData){
		if (manager.IsBusy){
			return ExecutionResult.Failure("Still waiting for the last action to finish.");}
		return ValidateAction(actionData);}

	protected abstract ExecutionResult ValidateAction(ActionJData actionData);
}

public abstract class BusyAction<TData> : NeuroAction<TData> {

	protected readonly BombManager manager;
	protected BusyAction(BombManager bombManager){manager = bombManager;}

	protected sealed override ExecutionResult Validate(ActionJData actionData, out TData parsedData){
		if (manager.IsBusy){
			parsedData = default(TData);
			return ExecutionResult.Failure("Still waiting for the last action to finish, try again in a moment.");}
		return ValidateAction(actionData, out parsedData);}

	protected abstract ExecutionResult ValidateAction(ActionJData actionData, out TData parsedData);
}
