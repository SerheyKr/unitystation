using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using ScriptableObjects.Atmospherics;
using TileManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Atmospherics
{
	public static class Gas
	{
		//These are here as its easier to put Gas.Gases than GasesSingleton.Instance.Gases
		public static Dictionary<int, GasSO> Gases => GasesSingleton.Instance.Gases;
		public static Dictionary<Reagent, GasSO> ReagentToGas => GasesSingleton.Instance.ReagentToGas;
		public static Dictionary<GasSO, Reagent> GasToReagent => GasesSingleton.Instance.GasToReagent;

		// Gas constant
		public const float R = 8.3144598f;

		public static GasSO Plasma => GasesSingleton.Instance.Plasma;
		public static GasSO Oxygen => GasesSingleton.Instance.Oxygen;
		public static GasSO Nitrogen => GasesSingleton.Instance.Nitrogen;
		public static GasSO CarbonDioxide => GasesSingleton.Instance.CarbonDioxide;

		public static GasSO NitrousOxide => GasesSingleton.Instance.NitrousOxide;
		public static GasSO Hydrogen => GasesSingleton.Instance.Hydrogen;
		public static GasSO WaterVapor => GasesSingleton.Instance.WaterVapor;
		public static GasSO BZ => GasesSingleton.Instance.BZ;
		public static GasSO Miasma => GasesSingleton.Instance.Miasma;
		public static GasSO Nitryl => GasesSingleton.Instance.Nitryl;
		public static GasSO Tritium => GasesSingleton.Instance.Tritium;
		public static GasSO HyperNoblium => GasesSingleton.Instance.HyperNoblium;
		public static GasSO Stimulum => GasesSingleton.Instance.Stimulum;
		public static GasSO Pluoxium => GasesSingleton.Instance.Pluoxium;
		public static GasSO Freon => GasesSingleton.Instance.Freon;
		public static GasSO Smoke => GasesSingleton.Instance.Smoke;
		public static GasSO Ash => GasesSingleton.Instance.Ash;
		public static GasSO CarbonMonoxide => GasesSingleton.Instance.CarbonMonoxide;
	}

	[Serializable]
	public class GasData
	{

		//Used for quick iteration
		[FormerlySerializedAs("GasesArray")]
		public List<GasValues> EditorGasesArray = new List<GasValues>();


		[HideInInspector]
		public float[] GetGasesArray
		{
			get
			{
				if (EditorGasesArray != null)
				{
					foreach (var GV in EditorGasesArray)
					{
						if (GV.GasSO == null || GV.Moles == 0) continue;
						if (GV.GasSO >= GasesArray.Length)
						{
							Array.Resize(ref GasesArray,  GV.GasSO+1);
						}
						GasesArray[GV.GasSO] = GV.Moles;
					}

					EditorGasesArray = null;
				}
				return GasesArray;
			}
			set
			{
				GasesArray = value;
			}
		}

		public float[] GasesArray = Array.Empty<float>();
		public void Clear()
		{
			lock (GetGasesArray)
			{
				for (int i = 0; i < GetGasesArray.Length; i++)
				{
					GetGasesArray[i] = 0;
				}
			}
		}
	}

	[Serializable]
	public class GasValues
	{
		public GasSO GasSO;

		//Moles of this gas type
		public float Moles;

		public void Pool()
		{
			GasSO = null;
			Moles = 0;
			lock (AtmosUtils.PooledGasValues)
			{
				AtmosUtils.PooledGasValues.Add(this);
			}
		}
	}
}