using NeuroSdk.Actions;

public interface IBombModuleHandler {
	string GetContext();
	string GetStatus();
	void RegisterActions(ActionWindow window, BombManager manager);
}

public abstract class BombModuleHandler : IBombModuleHandler {

	public abstract string GetContext();

	public virtual string GetStatus()
	{
		return GetContext();
	}

	public abstract void RegisterActions(
		ActionWindow window,
		BombManager manager);
}