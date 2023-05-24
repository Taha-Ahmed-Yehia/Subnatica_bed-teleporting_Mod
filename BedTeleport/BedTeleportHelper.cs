using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BedTeleport
{
    class BedTeleportHelper
    {
        public static List<Button> GetButtonsFromMenuGameObject(GameObject menuGameObject)
        {
            List<Button> ButtonsList = new List<Button>();
            var buttons = menuGameObject.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.gameObject.activeSelf )
                {
                    ButtonsList.Add(button);
                    button.gameObject.SetActive(false);
                }
            }
            return ButtonsList;
        }
        public static List<Bed> GetAllBeds()
        {
            List<Bed> bedsList = new List<Bed>();
            var bedsArray = Object.FindObjectsOfType<Bed>();
            foreach (var item in bedsArray)
            {
                bedsList.Add(item);
            }
            return bedsList;
        }
        public static List<BeaconLabel> GetBeacons()
        {
            List<BeaconLabel> BeaconLabels = new List<BeaconLabel>();
            var BeaconLabelsArray = Object.FindObjectsOfType<BeaconLabel>();
            foreach (var item in BeaconLabelsArray)
            {
                BeaconLabels.Add(item);
            }
            return BeaconLabels;
        }
    }
}
