How to use
==========
Simply instantiate from the prefab and use.  By default, the hand is a right hand.  Uncheck the "Right Hand" checkbox in the hand controller script to create a left hand.  Note that the left hand is created by negating the model's X scale at runtime.
 



How it works
============
The rigged hand comes with a pair of animation poses: a "resting" pose and a "clinched" pose.  A pair of avatar masks are used to layer the clinching animation onto the rest animation.  The "activation" mask, masks everything except the index finger.  Thus, the resulting animation affects only the index finger, corresponding to the VR "activation" action.  The "select" mask masks everything except the middle, ring and pinky fingers.  Applying this mask results in an animation of these fingers, corresponding to the VR "select" action.

"Activation" can be thought of as "triggering" gesture and "Selecting" can be thought of as "gripping" gesture.

A few skin tones are provided in the Materials folder.
