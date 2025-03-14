using Antagonists;
using Mirror;
using Player;
using System.Collections.Generic;
using Systems.Character;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Changeling")]
	public class Changeling : Antagonist
	{
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			if (spawnRequest.CharacterSettings.Species.ToLower().Contains("cow") || spawnRequest.CharacterSettings.Species.ToLower().Contains("monkey"))
			{
				var races = RaceSOSingleton.Instance.Races;
				var racesToAdd = new List<PlayerHealthData>();

				foreach (PlayerHealthData x in races)
				{
					if (x.name.ToLower().Contains("monkey") == false)
					{
						racesToAdd.Add(x);
					}
				}

				CharacterSheet chSh = CharacterSheet.GenerateRandomCharacter(racesToAdd);
				chSh.SerialisedBodyPartCustom = new List<UI.CharacterCreator.CustomisationStorage>
				{
					new UI.CharacterCreator.CustomisationStorage()
				};

				return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, chSh);
			}
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = NewMind.Body.playerHealth.brain.gameObject.GetComponent<ChangelingMain>();
			ch.NetEnable();

			PlayerSpawn.TransferOwnershipFromToConnection(NewMind.ControlledBy, null, ch.gameObject.GetComponent<NetworkIdentity>());

			ch.Init(NewMind);
		}
	}
}