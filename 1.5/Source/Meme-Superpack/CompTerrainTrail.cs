using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSS.MemeSuperpack;

public class CompTerrainTrail : ThingComp
{
	private IntVec3 _priorLocation = IntVec3.Invalid;
	public bool terrainOn = true;

	private CompProperties_TerrainTrail Props => (CompProperties_TerrainTrail)props;

	public override void CompTick()
	{
		base.CompTick();
		if (!terrainOn || parent.Position == _priorLocation || Find.TickManager.TicksGame % Props.spawnTicks != 0 ||
		    !parent.Position.IsValid) return;
		_priorLocation = parent.Position;
		parent.Map.terrainGrid.SetTerrain(_priorLocation, Props.terrain);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (var gizmo in base.CompGetGizmosExtra() ?? [])
		{
			yield return gizmo;
		}

		if (parent.Faction != Faction.OfPlayer) yield break;
		yield return new Command_Toggle
		{
			defaultLabel = "MSSMeme_SpawnTerrain".Translate(Props.terrain.label),
			defaultDesc = "MSSMeme_SpawnTerrainDesc".Translate(Props.terrain.LabelCap),
			icon = TexCommand.ToggleVent,
			isActive = () => terrainOn,
			toggleAction = () => terrainOn = !terrainOn
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref terrainOn, "terrainOn", true);
	}
}
