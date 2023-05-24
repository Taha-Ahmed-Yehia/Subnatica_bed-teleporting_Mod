using HarmonyLib;
using UnityEngine;
using UWE;

namespace BedTeleport
{
    class BedTeleportMenu : uGUI_InputGroup, uGUI_IButtonReceiver
    {
        private uGUI_GraphicRaycaster interactionRaycaster;

		protected override void Awake()
		{
			base.Awake();
			interactionRaycaster = FindObjectOfType<uGUI_GraphicRaycaster>();
			if (interactionRaycaster) {
				interactionRaycaster.updateRaycasterStatusDelegate += UpdateRaycasterStatus;
				BedTeleport.Dbgl("interactionRaycaster setuped.");
			}

			//interactionRaycaster = (uGUI_GraphicRaycaster)AccessTools.Field(typeof(IngameMenu), "interactionRaycaster").GetValue(IngameMenu.main);
			//interactionRaycaster.updateRaycasterStatusDelegate = new uGUI_GraphicRaycaster.UpdateRaycasterStatus(UpdateRaycasterStatus);
		}

		public bool OnButtonDown(GameInput.Button button)
        {
			bool result;
			if (button == GameInput.Button.UIMenu)
			{
				Close();
				GameInput.ClearInput();
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		private void UpdateRaycasterStatus(uGUI_GraphicRaycaster raycaster)
		{
			if (GameInput.IsPrimaryDeviceGamepad() && !VROptions.GetUseGazeBasedCursor())
			{
				raycaster.enabled = false;
			}
			else
			{
				raycaster.enabled = base.focused;
			}
		}

		protected virtual void OnEnable()
		{
			BedTeleport.Dbgl("OnEnable Start");
			uGUI_LegendBar.ClearButtons();
			uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, false, " / ", true), Language.main.GetFormat("Back"));
			uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, false, " / ", true), Language.main.GetFormat("ItemSelectorSelect"));
			BedTeleport.Dbgl("OnEnable End");
		}
		
		protected override void OnDisable()
		{
			BedTeleport.Dbgl("Disabling Menu");
			base.OnDisable();
			uGUI_LegendBar.ClearButtons();
			if (interactionRaycaster)
			{
				interactionRaycaster.updateRaycasterStatusDelegate -= UpdateRaycasterStatus;
				BedTeleport.Dbgl("interactionRaycaster deleted.");
			}
			Destroy(gameObject);
			BedTeleport.Dbgl("Menu Disabled");
		}

		public override void OnSelect(bool lockMovement)
		{
			BedTeleport.Dbgl("Selecting Menu");
			base.OnSelect(lockMovement);
			gameObject.SetActive(true);
			FreezeTime.Begin(FreezeTime.Id.IngameMenu);
			//Utils.lockCursor = false;
			BedTeleport.Dbgl("Selected Menu");
		}

		public override void OnDeselect()
		{
			BedTeleport.Dbgl("Deselecting Menu");
			base.OnDeselect();
			FreezeTime.End(FreezeTime.Id.IngameMenu);
			IngameMenu.main.Close();
			Destroy(gameObject);
			BedTeleport.Dbgl("Menu Deselected");
		}

		private void Close()
		{
			BedTeleport.Dbgl("Closing menu");
			IngameMenu.main.Close();
			Deselect();
			Destroy(gameObject);
		}
    }
}
