using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace MSS.MemeSuperpack.RimRim;

public class GameComponent_RimRimTracker : GameComponent
{
	private static int _nextPossibleLetterTick = 0;
	private static Building _cachedTelly = null;

	public override void GameComponentTick()
	{
		base.GameComponentTick();

		var ticksGame = Find.TickManager.TicksGame;
		if (
			_nextPossibleLetterTick > ticksGame
			|| ticksGame % 1000 != 0
			|| GenLocalDate.HourOfDay(Find.CurrentMap) != 18
		)
			return;
		_nextPossibleLetterTick = ticksGame + 55000; // 22H
		if (
			!ModLister.IdeologyInstalled
			|| Find.FactionManager.OfPlayer.ideos.GetPrecept(MemeSuperPackDefOf.MSSMeme_RimRim_Demanded)
				== null
		)
			return;
		var pawns = Find
			.CurrentMap.mapPawns.FreeColonistsSpawned.Where(p =>
				p.ideo?.Ideo?.HasPrecept(MemeSuperPackDefOf.MSSMeme_RimRim_Demanded) ?? false
			)
			.ToList();
		if (!pawns.Any())
			return;
		if (
			MemeSuperpackMod.settings.autoRimRim
			&& Find.FactionManager.OfPlayer.ideos.GetPrecept(MemeSuperPackDefOf.MSSMeme_RimRim_Ritual)
				is Precept_Ritual ritual
		)
		{
			AutoStartRitual(Find.CurrentMap, ritual, pawns);
		}

		if (
			!MemeSuperpackMod.settings.whereRimRim
			|| !pawns.Any(p =>
				MemeSuperPackDefOf.MSSMeme_RimRim_Missed.Worker?.CurrentState(p).Active ?? false
			)
		)
			return;
		Find.LetterStack.ReceiveLetter(
			LetterMaker.MakeLetter(
				"MSSMeme_WhereRimRimLetter".TranslateSimple(),
				"MSSMeme_WhereRimRimLetterText".Translate(),
				LetterDefOf.NegativeEvent
			)
		);
	}

	[CanBeNull]
	public Building GetTelevisionTarget(Map map)
	{
		if (
			_cachedTelly is { Spawned: true }
			&& (_cachedTelly.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)
		)
			return _cachedTelly;
		_cachedTelly = map.listerBuildings.allBuildingsColonist.Find(b =>
			b.Spawned
			&& (b.def?.building?.joyKind ?? JoyKindDefOf.Social)
				== ThoughtWorker_RimRimDemanded.TelevisionJoyKind.Value
			&& (b.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)
		);
		return _cachedTelly;
	}

	public void AutoStartRitual(Map map, Precept_Ritual ritual, List<Pawn> pawns)
	{
		List<Pawn> freeTimePawns = GetPawnsInFreeTime(pawns);
		Building television = GetTelevisionTarget(map);

		if (television == null || freeTimePawns.Count == 0)
			return;
		RitualRoleAssignments assignments = new RitualRoleAssignments(
			ritual,
			new TargetInfo(television)
		);
		assignments.Setup(freeTimePawns, []);
		foreach (Pawn pawn in freeTimePawns)
		{
			assignments.TryAssignSpectate(pawn);
		}

		ritual.behavior.TryExecuteOn(
			television,
			freeTimePawns.First(),
			ritual,
			null,
			assignments,
			true
		);
	}

	public List<Pawn> GetPawnsInFreeTime(IEnumerable<Pawn> pawns) =>
		pawns
			.Where(pawn =>
				pawn.timetable.CurrentAssignment == TimeAssignmentDefOf.Anything
				|| pawn.timetable.CurrentAssignment == TimeAssignmentDefOf.Joy
					&& !pawn.DeadOrDowned
					&& !pawn.Drafted
					&& pawn.IsColonistPlayerControlled
			)
			.ToList();

	public GameComponent_RimRimTracker(Game game) { }
}
