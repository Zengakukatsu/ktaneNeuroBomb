using System;
using System.Collections.Generic;

public static class ModuleHandlerRegistry {

	private static readonly Dictionary<Type, Func<BombComponent, IBombModuleHandler>> factories = new Dictionary<Type, Func<BombComponent, IBombModuleHandler>>
	{
		{ typeof(WireSetComponent), comp => new WiresModuleHandler((WireSetComponent)comp) },
	};

	public static IBombModuleHandler Create(BombComponent component)
	{
		Func<BombComponent, IBombModuleHandler> factory;

		if (factories.TryGetValue(component.GetType(), out factory)){
			return factory(component);}

		return new GenericModuleHandler(component);
	}
}
