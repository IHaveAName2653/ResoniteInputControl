using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using System.Linq;
using System.Reflection;

namespace ResoniteInputControl;

using Renderite.Shared;

#if DEBUG
using ResoniteHotReloadLib;
#endif

public class ResoniteInputControl : ResoniteMod
{
	private static Assembly ModAssembly => typeof(ResoniteInputControl).Assembly;

	public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
	public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company;
	public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
	public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value!;

	internal static string HarmonyId => $"com.IHaveAName2653.{ModAssembly.GetName()}";

	private static readonly Harmony harmony = new(HarmonyId);

	public static ModConfiguration? Config;

	// Example Mod Config Key // Provides a "Mod Toggle" (assuming the functions implement it)
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<bool> shouldBeActive = new("IsActive", "If the mod should generate the dynvars", () => true);

	static ResoniteInputControl()
	{
		DebugFunc(() => $"Static Initializing {nameof(ResoniteInputControl)}...");
	}

	public override void OnEngineInit()
	{
#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif

		Config = GetConfiguration()!;

		harmony.PatchAll(ModAssembly);
	}


#if DEBUG
	static void BeforeHotReload()
	{
		harmony.UnpatchAll(HarmonyId);
	}

	static void OnHotReload(ResoniteMod modInstance)
	{
		harmony.PatchAll(ModAssembly);
	}
#endif
}
