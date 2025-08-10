using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSS.MemeSuperpack;

public class CompSpawnerFire : ThingComp
{
	private static Lazy<Texture2D> FireTexture =
		new(() => ContentFinder<Texture2D>.Get("UI/Misc/Flash"));

	private Stack<IntVec3> _priorLocations = new();
	private IntVec3 _priorLocation;
	private bool flameOn = MemeSuperpackMod.settings.fireTrails;
	private int flameUp = 1;

	private CompProperties_SpawnerFire Props => (CompProperties_SpawnerFire)props;

	private bool CanSpawnFire => parent is not Hive parentThing || parentThing.CompDormant.Awake;

	public override void CompTick()
	{
		if (!flameOn)
			return;
		base.CompTick();
		if (parent.Position != _priorLocation)
		{
			_priorLocation = parent.Position;
			_priorLocations.Push(_priorLocation);
		}

		if (!parent.IsHashIntervalTick(Props.spawnTicks / flameUp) || !CanSpawnFire)
			return;
		TrySpawnFire();
	}

	public void TrySpawnFire()
	{
		IntVec3 result = IntVec3.Invalid;
		if (
			parent.Map == null
			|| (
				!Props.behindOnly
				&& !CellFinder.TryFindRandomReachableNearbyCell(
					parent.Position,
					parent.Map,
					Props.spawnRadius,
					TraverseParms.For(TraverseMode.PassAllDestroyableThings),
					x => x.TerrainFlammableNow(parent.Map),
					x => true,
					out result
				)
			)
		)
			return;
		if (Props.behindOnly)
		{
			int pops = Math.Min(_priorLocations.Count - 1, (int)Props.spawnRadius);
			while (pops > 0)
			{
				result = _priorLocations.Pop();
				pops--;
			}
		}

		if (result != IntVec3.Invalid)
			FireUtility.TryStartFireIn(result, parent.Map, Props.fireSize * flameUp, parent);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (var gizmo in base.CompGetGizmosExtra() ?? [])
		{
			yield return gizmo;
		}

		if (parent.Faction != Faction.OfPlayer)
			yield break;
		yield return new Command_Toggle
		{
			defaultLabel = "MSSMeme_SpawnFire".Translate(),
			defaultDesc = "MSSMeme_SpawnFireDesc".Translate(),
			icon = FireTexture.Value,
			isActive = () => flameOn,
			toggleAction = () => flameOn = !flameOn
		};

		yield return new Command_Action()
		{
			defaultLabel = "MSSMeme_SpawnFireSize".Translate(flameUp.ToString()),
			defaultDesc = "MSSMeme_SpawnFireSizeDesc".Translate(),
			icon = FireTexture.Value,
			action = () => flameUp = flameUp == 3 ? 1 : flameUp + 1
		};
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		flameOn = MemeSuperpackMod.settings.fireTrails;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref flameOn, "flameOn", true);
		Scribe_Values.Look(ref flameUp, "flameUp", 1);
	}
}
