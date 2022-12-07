using System;
using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class AtmosUtils
	{
		public static List<GasValues> PooledGasValues = new List<GasValues>();

		public static GasValues GetGasValues()
		{
			lock (PooledGasValues)
			{
				if (PooledGasValues.Count > 0)
				{
					var QEntry = PooledGasValues[0];
					PooledGasValues.RemoveAt(0);
					return QEntry;
				}
			}

			return new GasValues();
		}


		public static List<GasValuesList> PooledGasValuesLists = new List<GasValuesList>();

		public static GasValuesList GetGasValuesList()
		{
			lock (PooledGasValuesLists)
			{
				if (PooledGasValuesLists.Count > 0)
				{
					var QEntry = PooledGasValuesLists[0];
					PooledGasValuesLists.RemoveAt(0);
					if (QEntry == null)
					{
						return new GasValuesList();
					}


					for (int i = 0; i < QEntry.List.Length; i++)
					{
						QEntry.List[i] = 0;
					}


					return QEntry;
				}
			}

			return new GasValuesList();
		}

		public class GasValuesList
		{
			public float[] List = new float[0];

			public void Pool()
			{
				for (int i = 0; i < List.Length; i++)
				{
					List[i] = 0;
				}
				lock (PooledGasValuesLists)
				{
					PooledGasValuesLists.Add(this);
				}
			}
		}


		public static GasValuesList CopyGasArray(GasData GasData)
		{
			var List = GetGasValuesList();



			lock (GasData.GetGasesArray) //no Double lock
			{
				if (GasData.GetGasesArray.Length > List.List.Length)
				{
					Array.Resize(ref List.List, GasData.GetGasesArray.Length);
				}

				for (var index = 0; index < GasData.GetGasesArray.Length; index++)
				{
					List.List[index] = GasData.GetGasesArray[index];
				}
			}


			return List;
		}


		public static readonly Vector2Int MINUS_ONE = new Vector2Int(-1, -1);

		public static float CalcPressure(float volume, float moles, float temperature)
		{
			if (temperature > 0 && moles > 0 && volume > 0)
			{
				return moles * Gas.R * temperature / volume / 1000;
			}

			return 0;
		}

		public static float CalcVolume(float pressure, float moles, float temperature)
		{
			if (temperature > 0 && pressure > 0 && moles > 0)
			{
				return moles * Gas.R * temperature / pressure;
			}

			return 0;
		}

		public static float CalcMoles(float pressure, float volume, float temperature)
		{
			if (temperature > 0 && pressure > 0 && volume > 0)
			{
				return pressure * volume / (Gas.R * temperature) * 1000;
			}

			return 0;
		}

		public static float CalcTemperature(float pressure, float volume, float moles)
		{
			if (volume > 0 && pressure > 0 && moles > 0)
			{
				return pressure * volume / (Gas.R * moles) * 1000;
			}

			return AtmosDefines.SPACE_TEMPERATURE; //space radiation
		}

		/// <summary>
		/// Total moles of this array of gases
		/// </summary>
		public static float Sum(this GasData data)
		{
			var total = 0f;

			lock (data.GetGasesArray) //no Double lock
			{
				foreach (var gas in data.GetGasesArray)
				{
					total += gas;
				}
			}


			return total;
		}

		/// <summary>
		/// Checks to see if the gas mix contains a specific gas
		/// </summary>
		public static bool HasGasType(this GasData data, GasSO gasType)
		{
			var Index = gasType.thisIndex;
			if (Index >= data.GetGasesArray.Length )
			{
				return false;

			}
			else
			{
				if (data.GetGasesArray[Index] > 0)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static float GetGasMoles(this GasData data, GasSO gasType)
		{
			return GetGasType(data, gasType);
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static float GetGasMoles(this GasData data, int gasType)
		{
			return GetGasType(data, gasType);
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static void GetGasMoles(this GasData data, GasSO gasType, out float gasMoles)
		{
			gasMoles = GetGasMoles(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static void GetGasType(this GasData data, GasSO gasType, out float gasData)
		{

			gasData = GetGasType(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static float GetGasType(this GasData data, GasSO gasType)
		{
			return data.GetGasType(gasType.thisIndex);


		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static float GetGasType(this GasData data, int gasType)
		{
			var Index = gasType;
			if (Index >= data.GetGasesArray.Length )
			{
				return 0;
			}
			else
			{
				return data.GetGasesArray[Index];
			}
		}

		/// <summary>
		/// Adds/Removes moles for a specific gas in the gas data
		/// </summary>
		public static void ChangeMoles(this GasData data, GasSO gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, true);
		}

		public static void ChangeMoles(this GasData data, int gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, true);
		}

		/// <summary>
		/// Sets moles for a specific gas to a specific value in the gas data
		/// </summary>
		public static void SetMoles(this GasData data, GasSO gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, false);
		}

		/// <summary>
		/// Sets moles for a specific gas to a specific value in the gas data
		/// </summary>
		public static void SetMoles(this GasData data, int gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, false);
		}

		private static void InternalSetMoles(GasData data, int gasType, float moles, bool isChange)
		{
			lock (data.GetGasesArray) //Because it gets the gas and it could be added in between this
			{
				//Try to get gas value if already inside mix
				//GetGasType(data, gasType, out var gas);
				var Index = gasType;


				if (gasType >= data.GetGasesArray.Length)
				{

					//Dont add new data for negative moles
					if (Math.Sign(moles) == -1) return;

					//Dont add if approx 0 or below threshold
					if (moles is 0 or <= AtmosConstants.MinPressureDifference) return;
					Array.Resize(ref data.GasesArray,  Index+1);
				}

				if (isChange)
				{
					data.GetGasesArray[Index] += moles;
				}
				else
				{
					data.GetGasesArray[Index] = moles;
				}

				//Remove gas from mix if less than threshold
				if (data.GetGasesArray[Index] <= AtmosConstants.MinPressureDifference)
				{
					data.RemoveGasType(gasType);
				}
			}
		}

		/// <summary>
		/// Removes a specific gas type
		/// </summary>
		public static void RemoveGasType(this GasData data, int gasType)
		{
			lock (data.GetGasesArray) //no Double lock
			{
				data.GetGasesArray[gasType] = 0;
			}
		}

		/// <summary>
		/// Copies the array, creating new references
		/// </summary>
		/// <param name="oldData"></param>
		public static GasData Copy(this GasData oldData)
		{
			var newGasData = new GasData();

			var List = CopyGasArray(oldData);

			for (var index = 0; index < List.List.Length; index++)
			{
				newGasData.SetMoles(index, List.List[index]);
			}

			List.Pool();

			return newGasData;
		}


		/// <summary>
		/// Copies the array, creating new references
		/// </summary>
		/// <param name="oldData"></param>
		public static GasData CopyTo(this GasData oldData, GasData CopyTo)
		{
			CopyTo.Clear();

			var List = CopyGasArray(oldData);

			for (var index = 0; index < List.List.Length; index++)
			{
				CopyTo.SetMoles(index, List.List[index]);
			}

			List.Pool();

			return CopyTo;
		}
	}
}