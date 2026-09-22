using System;
using System.Collections.Generic;
using Assets.Scripts.Components.VennWire;

public static class ModuleHandlerRegistry {

	// This is where you add new Handlers for discovery by the BombManager
	private static readonly Dictionary<Type, Func<BombComponent, IBombModuleHandler>> factories = new Dictionary<Type, Func<BombComponent, IBombModuleHandler>>
	{
		{ typeof(WireSetComponent), 		comp => new WiresModuleHandler((WireSetComponent)comp) },
        { typeof(KeypadComponent),			comp => new KeypadModuleHandler((KeypadComponent)comp) },
	    { typeof(ButtonComponent),			comp => new ButtonModuleHandler((ButtonComponent)comp) },
	    { typeof(SimonComponent),			comp => new SimonModuleHandler((SimonComponent)comp) },
	    { typeof(MemoryComponent),			comp => new MemoryModuleHandler((MemoryComponent)comp) },
	    { typeof(WhosOnFirstComponent),		comp => new WhoModuleHandler((WhosOnFirstComponent)comp) },
	    { typeof(VennWireComponent),		comp => new ComplicatedWiresModuleHandler((VennWireComponent)comp) },
	    { typeof(PasswordComponent),		comp => new PasswordModuleHandler((PasswordComponent)comp) },
	    { typeof(MorseCodeComponent),		comp => new MorseModuleHandler((MorseCodeComponent)comp) },
		{ typeof(WireSequenceComponent),	comp => new WireSequenceModuleHandler((WireSequenceComponent)comp) },
	    { typeof(InvisibleWallsComponent),	comp => new MazeModuleHandler((InvisibleWallsComponent)comp) }
	};

	public static IBombModuleHandler Create(BombComponent component)
	{
		Func<BombComponent, IBombModuleHandler> factory;

		if (factories.TryGetValue(component.GetType(), out factory)){
			return factory(component);}

		return new GenericModuleHandler(component);
	}
}
