using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace HydroponicsGrowthSync
{
	public class PlantGrowerGrowthSyncer : MapComponent
	{
		private List<List<Thing>> plantGrowerGroups = new List<List<Thing>>();

		[TweakValue("PlantGrowerGrowthSyncer")]
		public static bool DrawGroups = false;

		[TweakValue("PlantGrowerGrowthSyncer", 0f, 2.2f)]
		public static float SyncRatePerFullGrowth = 1.15f;

		public PlantGrowerGrowthSyncer(Map map) : base(map)
		{
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % 2000 == 0)
			{
				BuildPlantGrowerGroups();
				SyncAllGroups();
			}
		}

		public override void MapComponentOnGUI()
		{
			if (DrawGroups)
			{
				for (int i = 0; i < plantGrowerGroups.Count; i++)
				{
					for (int j = 0; j < plantGrowerGroups[i].Count; j++)
					{
						Vector2 labelPosition = UI.MapToUIPosition(plantGrowerGroups[i][j].DrawPos);
						Rect labelRect = new Rect(labelPosition.x, labelPosition.y, 999f, 50f);
						Text.Font = GameFont.Medium;
						Widgets.Label(labelRect, i.ToString());
						Text.Font = GameFont.Small;
					}
				}
			}
		}

		private void BuildPlantGrowerGroups()
		{
			plantGrowerGroups.Clear();
			IEnumerable<Thing> allPlantGrowers = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Where(x => x is Building_PlantGrower);
			HashSet<Thing> alreadyGrouped = new HashSet<Thing>();
            HashSet<Thing> queue = new HashSet<Thing>();
			foreach (Thing grower in allPlantGrowers)
			{
				if (alreadyGrouped.Contains(grower)) continue;
				List<Thing> growerGroup = new List<Thing>();
                queue.Add(grower);
				while (queue.Count > 0)
				{
					alreadyGrouped.AddRange(queue);
					growerGroup.AddRange(queue);
					HashSet<Thing> oldQueue = new HashSet<Thing>(queue);
					queue.Clear();
					foreach (Building_PlantGrower adjRoot in oldQueue)
					{
						foreach (Building_PlantGrower adjAgainst in allPlantGrowers)
						{
							if (alreadyGrouped.Contains(adjAgainst)) continue;
							if (adjRoot.CanBeGroupedTo(adjAgainst))
							{
								queue.Add(adjAgainst);
							}
						}
					}
				}
				plantGrowerGroups.Add(growerGroup);
			}
		}

		private void SyncAllGroups()
		{
			for (int i = 0; i < plantGrowerGroups.Count; i++)
			{
				ThingDef groupPlantDef = (plantGrowerGroups[i][0] as Building_PlantGrower).GetPlantDefToGrow();
				/*
				 * NOTE
				 * The following code is heavily based on the plant syncing code in Plant Growth Sync by Lanilor.
				 * This is so that people using this alongside Plant Growth Sync will have their crops synced in a similar way.
				 * 
				 * LINKS
				 * https://steamcommunity.com/sharedfiles/filedetails/?id=1454228967&searchtext=Plant+Growth+Sync
				 * https://github.com/Lanilor/Simple-Mods-Collection
				 * */

				List<Plant> plantsToSync = new List<Plant>();
				float averageGrowth = 0f;
				for (int j = 0; j < plantGrowerGroups[i].Count; j++)
				{
					Building_PlantGrower plantGrower = plantGrowerGroups[i][j] as Building_PlantGrower;
					foreach (Plant plant in plantGrower.PlantsOnMe)
					{
						if (plant.def == groupPlantDef && plant.IsCrop && plant.LifeStage == PlantLifeStage.Growing)
						{
							plantsToSync.Add(plant);
							averageGrowth += plant.Growth;
						}
					}
				}
				if (plantsToSync.Count < 2) continue;
				averageGrowth /= plantsToSync.Count;

				float marginFromAverage = SyncRatePerFullGrowth / (groupPlantDef.plant.growDays * 30f);
				int numUnderAverage = 0;
				int numAboveAverage = 0;
				for (int k = plantsToSync.Count - 1; 0 <= k; k--)
				{
					if (Mathf.Abs(averageGrowth - plantsToSync[k].Growth) <= marginFromAverage)
					{
						plantsToSync[k].Growth = averageGrowth;
						plantsToSync.RemoveAt(k);
					}
					else
					{
						if (plantsToSync[k].Growth > averageGrowth) numAboveAverage++;
						if (plantsToSync[k].Growth < averageGrowth) numUnderAverage++;
					}
				}

				float underMultiplier = 1f;
				float aboveMultiplier = 1f;
				if (numUnderAverage > 0 && numAboveAverage > 0)
				{
					if (numUnderAverage > numAboveAverage)
					{
						underMultiplier = numAboveAverage / (float)numUnderAverage;
					}
					else
					{
						aboveMultiplier = numUnderAverage / (float)numAboveAverage;
					}
				}

				for (int l = 0; l < plantsToSync.Count; l++)
				{
					if (plantsToSync[l].Growth < averageGrowth) plantsToSync[l].Growth += marginFromAverage * underMultiplier;
					if (plantsToSync[l].Growth > averageGrowth) plantsToSync[l].Growth -= marginFromAverage * aboveMultiplier;
				}
			}
		}
	}
}
