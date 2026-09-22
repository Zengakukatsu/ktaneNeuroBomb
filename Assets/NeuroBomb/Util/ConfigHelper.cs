using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class ConfigHelper
{
	private static JObject data;
	private static bool loaded;

	public static T Get<T>(T fallback, params string[] path)
	{
		EnsureLoaded();

		try{
			JToken current = data;

			foreach (string key in path){
				JObject current_object = current as JObject;

				if (current_object == null){return fallback;}
				current = current_object[key];
				if (current == null){return fallback;}
			}
			return current.ToObject<T>();
		}
		catch (Exception exception)
		{
			Debug.LogWarning(string.Format("[NeuroBomb] Could not read config value {0}: {1}", string.Join(".", path), exception.Message));
			return fallback;
		}
	}

	public static void Reload()
	{
		loaded = true;
		data = new JObject();

		try{
			KMModSettings settings = UnityEngine.Object.FindObjectOfType<KMModSettings>();

			if (settings == null){
				Debug.LogWarning("[NeuroBomb] KMModSettings was not found. Using defaults.");
				return;}

			settings.RefreshSettings();

			if (string.IsNullOrEmpty(settings.Settings)){
				Debug.LogWarning("[NeuroBomb] modSettings.json was empty. Using defaults.");
				return;}

			data = JObject.Parse(settings.Settings);
		}
		catch (Exception exception)
		{
			Debug.LogWarning(
				"[NeuroBomb] Could not load modSettings.json: " +
				exception.Message);
		}
	}

	private static void EnsureLoaded()
	{
		if (!loaded){Reload();}
	}
}