How to use
==========
1 - Import the "VR Hands" folder to your XR project.  There should be an LHand and RHand prefab in the top level folder.  (FYI: The difference between these two are 1) the "Right Hand" checkbox in the Hand Controller script is unchecked for the LHand and 2) the transforms of the prefab's child Hand object is slightly different for the L and R hands.)
2 - Set the XR Left and Right hand controllers by copying the prefabs to their respective controllers:
	XR Rig->LeftHand Controller->XR Controller (Action-based)->Model Prefab
	XR Rig->RightHand Controller->XR Controller (Action-based)->Model Prefab
3 - If you need to adjust the attach point for the hand, you may do so in the XRInteractor inspector or you can open the LHand or RHand prefab and tweak the transform of the "Hand" child object.

