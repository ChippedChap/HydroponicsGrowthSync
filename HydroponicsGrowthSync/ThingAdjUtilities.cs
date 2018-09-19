using RimWorld;
using Verse;

namespace HydroponicsGrowthSync
{
	static class ThingAdjUtilities
	{
		public static bool CanBeGroupedTo(this Building_PlantGrower lhs, Building_PlantGrower rhs)
		{
			return lhs.IsConnectedTo(rhs) && lhs.GetPlantDefToGrow() == rhs.GetPlantDefToGrow();
		}

		public static bool IsConnectedTo(this Thing lhs, Thing rhs)
		{
			CellRect[] lhsAdjacencyRects = lhs.AdjacencyRects();
			foreach (IntVec3 occupiedTile in rhs.OccupiedRect())
			{
				for (int i = 0; i < 2; i++)
				{
					if (lhsAdjacencyRects[i].Contains(occupiedTile)) return true;
				}
			}
			return false;
		}

		public static CellRect[] AdjacencyRects(this Thing thing)
		{
			CellRect[] rects = new CellRect[2];
			CellRect occupiedRect = thing.OccupiedRect();
			rects[0] = new CellRect(occupiedRect.minX - 1, occupiedRect.minZ, occupiedRect.Width + 2, occupiedRect.Height);
			rects[1] = new CellRect(occupiedRect.minX, occupiedRect.minZ - 1, occupiedRect.Width, occupiedRect.Height + 2);
			return rects;
		}
	}
}
