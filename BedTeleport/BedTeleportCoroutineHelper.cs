using System.Collections;
using UnityEngine;
using UWE;

namespace BedTeleport
{
    class BedTeleportCoroutineHelper : MonoBehaviour
    {
		public void GoToLocation(Bed sourceBed, Bed destBed, bool gotoImmediate)
        {
			StartCoroutine(_GoToLocation(sourceBed, destBed, gotoImmediate));
		}

		private IEnumerator _GoToLocation(Bed sourceBed, Bed destBed, bool gotoImmediate)
		{
			BedTeleport.Dbgl($"Teleporting Player.");

			Vector3 position = destBed.transform.position + new Vector3(0f, 2.5f, 0f);
			Vector3 dest = position;
			Vector3 vector = dest - sourceBed.transform.position;
			Vector3 direction = vector.normalized;
			float magnitude = vector.magnitude;

			if (!gotoImmediate)
			{
				if (BedTeleport.playSound.Value)
				{
					Player.main.teleportingLoopSound.Play();
				}
				float num = 2.5f;
				float travelSpeed = 100f; // in future update put travelspeed as value input in SMLHelper menu
				if (magnitude / travelSpeed > num)
				{
					travelSpeed = magnitude / num;
				}
				//ESTime to make sure while loop break if something brevented it from breaking allowing user play normally.
				float ESTime = (magnitude / travelSpeed) + 5;
				BedTeleport.Dbgl($"ESTime = {ESTime}");
				Player.main.playerController.SetEnabled(false);

				//TODO: Add teleport effect screen
				while (ESTime > 0)
				{
					var deltaTime = Time.deltaTime;
					ESTime -= deltaTime;

					Vector3 position2 = Player.main.transform.position;
					float magnitude2 = (dest - position2).magnitude;
					float deltaTravelSpeed = travelSpeed * deltaTime;
					if (magnitude2 < deltaTravelSpeed)
					{
						break;
					}

					Vector3 position3 = position2 + direction * deltaTravelSpeed;
					Player.main.SetPosition(position3);
					yield return CoroutineUtils.waitForNextFrame;
				}

				if (BedTeleport.playSound.Value)
				{
					Player.main.teleportingLoopSound.Stop(0);
				}
			}

			Player.main.SetPosition(dest);

			if (position.y > 0f)
			{
				float travelSpeed2 = 15f;
				new Bounds(position, Vector3.zero);
				while (!LargeWorldStreamer.main.IsWorldSettled())
				{
					travelSpeed2 -= Time.deltaTime;
					if (travelSpeed2 < 0f)
					{
						break;
					}
					yield return CoroutineUtils.waitForNextFrame;
				}
			}

			Player.main.OnPlayerPositionCheat();
			Player.main.SetCurrentSub(destBed.GetComponentInParent<SubRoot>(), true);
			Player.main.playerController.SetEnabled(true);

			BedTeleport.Dbgl($"Player Teleported.");
			Destroy(gameObject);
		}

	}
}
