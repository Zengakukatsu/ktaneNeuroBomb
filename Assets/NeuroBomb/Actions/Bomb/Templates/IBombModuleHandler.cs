using NeuroSdk.Actions;

public interface IBombModuleHandler {
	string GetContext();
	void RegisterActions(ActionWindow window, BombManager manager);
}
