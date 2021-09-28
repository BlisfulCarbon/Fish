#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

// This example shows how to use the CSharp animator generator. Specifically, this class shows the "scriptable object"
// animator type. This type uses scriptable objects to serialize it's data. Since this is a scriptable object, you need
// to provide a way create an asset of this type. See the "Create" static method below. This type also requires one file per 
// scriptable object(like all scriptable objects in Unity). For this reason I recommend using the CSharpAnimator "Class"
// type instead, since you don't need to do these things.
public class CSharpAnimatorExamples : CSharpAnimator
{
	// Here we can declare serializable fields just like any monobehaviour or scriptable object class. These will appear in the animator's
	// inspector when we create it. We define motions here so we can assign them to our states.
	[SerializeField]
	public Motion idleMotion;
    [SerializeField]
    public Motion runMotion;
    [SerializeField]
    public Motion jumpMotion;

	[SerializeField]
	public float speedForRun = 5.0f;

	// You can create a method like this to make the type appear in the Unity editor.
	[MenuItem("Assets/Create/CSharpAnimator/Scriptable Object Basic Test")]
	public static void Create()
	{
		CreateWithType(typeof(CSharpAnimatorExamples));
	}

	// In order to generate the animator, we need to override the Construct() method. This method should
	// construct and return the AnimatorData so the animator asset can be generated.
	public override AnimatorData Construct()
	{
		// This is how parameters are declared. These will be used to determine which animation should be playing and how. Just like
		// in the animator window, we can create 4 different types of parameters; Float, Bool, Int and Trigger. We can also give them a default
		// value(except trigger) by passing it as the second argument.
		ParameterData speed = FloatParam("Speed", 1.0f);
		ParameterData jumpTrigger = TriggerParam("JumpTrigger");
		
		
		// Here we will define the entire animation layout. We do this by calling 'Animator' and then passing a graph and transition collection to
		// it. Each of these function calls returns a context object that can have methods invoked on it in order to define settings. This allows
		// you to only set the values you care about, and is more flexible than one massive function call with default parameters.
		Animator
		(
			// This is the root of the graph. Inside of here we can define only layers.
			Graph()
			[
				// Each layer typically takes a name, and it's often a good idea to define a default state.
				Layer("Base").DefaultState("Idle")
				[
					// States and sub state machines are defined within states. Check the advanced test for how to use state machines inside
					// of layers. Typically a state will take a name(necessary for transitions to reference it), and a motion.
					State("Idle", idleMotion),
					State("Run", runMotion),
					State("Jump", jumpMotion)
				]
			],

			// This call defines the group of transitions for the graph. Here we need to reference the states by a name string.
			// Each transition can take a Source(single source), a SourceMultiple(list multiple sources as parameters), a SourceRecursive(recursively add all states
			// below this state machine), any combination of these, as well a single destination state. All the usual transition options should be available through
			// additional method calls chained onto the return value of 'Transition()'. For example use:
			//
			// Transition().Source("Run").Destination("Jump").ExitTime(0.9f).TransitionTime(0.1f)
			//
			// In order to change the exit time and transition time of the transition.
			Transitions()
			[
				Transition().SourceMultiple("Idle", "Run").Destination("Jump")
				[
					// Conditions are listed here. Call 'Float', 'Bool', 'Int', or 'Trigger' functions in order to define them,
					// depending on the parameter type. Just like in the animator window, all these conditions must be met in order
					// for the transition to take place. Of course, any of these transitions can be based on constants in code,
					// or exposed editor fields, so we no longer have to duplicate magic numbers in every transitions like we do in
					// the animator window.
					Trigger(jumpTrigger)
				],
				// You can define as many transitions as needed by listing them here.
				Transition().Source("Jump").Destination("Idle").ExitTime(0.9f),

				Transition().Source("Idle").Destination("Run")
				[
					Float(speed, speedForRun, ConditionMode.Greater)
				],
				Transition().Source("Run").Destination("Idle")
				[
					Float(speed, speedForRun, ConditionMode.Less)
				]
			]
		);
		
		return currentData;
	}
}


// This section shows a few examples of how to use the 'Class' version of the animator generator. This is my preferred 
// type since, unlike the scriptable object type, they don't require their own file. They're missing none
// of the features of the scriptable object type as far as I'm aware. The only drawback I can see is I use a probably
// unsupported method of drawing the serialized fields, which might break in the future. Also, anytime you rename a type
// you'll have to reassign the type on the asset(you won't lose serialized data though, you'll need to explicitly press apply
// in order to change types, so it won't wipe your data automatically if it can't find the type).
//
// Basically you just need to inherit from CSharpAnimator_ClassBase, override and write the Construct method, and create a "CSharpAnimator Class"
// asset in the editor. Select your type from the drop down list and press generate.

// ==================================================================
// ========================== Basic Test ============================
// ==================================================================
public class ClassBasicTest : CSharpAnimator_ClassBase
{
	// Just like the scriptable object type, you can defined serialized fields in the class that will appear in the 
	// editor.
	[SerializeField]
	public Motion idleMotion;
    [SerializeField]
    public Motion runMotion;
    [SerializeField]
    public Motion jumpMotion;

	[SerializeField]
	public float speedForRun = 5.0f;

	// Again, we need to override the Construct method and return an animator data instance. Everything in this method
	// should appear exactly the same as the scriptable object type.
	public override AnimatorData Construct()
	{
		// This is how parameters are declared. These will be used to determine which animation should be playing and how. Just like
		// in the animator window, we can create 4 different types of parameters; Float, Bool, Int and Trigger. We can also give them a default
		// value(except trigger) by passing it as the second argument.
		ParameterData speed = FloatParam("Speed", 1.0f);
		ParameterData jumpTrigger = TriggerParam("JumpTrigger");
		
		
		// Here we will define the entire animation layout. We do this by calling 'Animator' and then passing a graph and transition collection to
		// it. Each of these function calls returns a context object that can have methods invoked on it in order to define settings. This allows
		// you to only set the values you care about, and is more flexible than one massive function call with default parameters.
		Animator
		(
			// This is the root of the graph. Inside of here we can define only layers.
			Graph()
			[
				// Each layer typically takes a name, and it's often a good idea to define a default state.
				Layer("Base").DefaultState("Idle")
				[
					// States and sub state machines are defined within states. Check the advanced test for how to use state machines inside
					// of layers. Typically a state will take a name(necessary for transitions to reference it), and a motion.
					State("Idle", idleMotion),
					State("Run", runMotion),
					State("Jump", jumpMotion)
				]
			],

			// This call defines the group of transitions for the graph. Here we need to (unfortunately) reference the states by a name string.
			// Each transition can take a Source(single source), a SourceMultiple(list multiple sources as parameters), a SourceRecursive(recursively add all states
			// below this state machine), any combination of these, as well a single destination state. All the usual transition options should be available through
			// additional method calls chained onto the return value of 'Transition()'. For example use:
			//
			// Transition().Source("Run").Destination("Jump").ExitTime(0.9f).TransitionTime(0.1f)
			//
			// In order to change the exit time and transition time of the transition.
			Transitions()
			[
				Transition().SourceMultiple("Idle", "Run").Destination("Jump")
				[
					// Conditions are listed here. Call 'Float', 'Bool', 'Int', or 'Trigger' functions in order to define them,
					// depending on the parameter type. Just like in the animator window, all these conditions must be met in order
					// for the transition to take place. Of course, any of these transitions can be based on constants in code,
					// or exposed editor fields, so we no longer have to duplicate magic numbers in every transitions like we do in
					// the animator window.
					Trigger(jumpTrigger)
				],
				// You can define as many transitions as needed by listing them here.
				Transition().Source("Jump").Destination("Idle").ExitTime(0.9f),

				Transition().Source("Idle").Destination("Run")
				[
					Float(speed, speedForRun, ConditionMode.Greater)
				],
				Transition().Source("Run").Destination("Idle")
				[
					Float(speed, speedForRun, ConditionMode.Less)
				]
			]
		);
		
		return currentData;
	}
}

// ==================================================================
// ======================== Advanced Test ===========================
// ==================================================================
public class ClassAdvancedTest : CSharpAnimator_ClassBase
{
	// Here we can declare editor fields that we can use while generating our animator. This works just like any monobehaviour or scripatble object.
	// The most common field will probably be motions, since we use those to assign an animation to a state. Of course we can use any data we want here,
	// for example we may have a (float) speed threshold the character must meet in order to be in the running state. This could be exposed to the editor
	// here and tweaked before the animator is generated.

	// A more complex example used here is the motion array called 'randomIdleMotions'. This allows us to drag and drop any number of idle animations we want in
	// the editor, and have the necessary states and transitions generated automatically using the GetRandomIdleStates and GetTransitionsBetweenRandomIdles functions.
	// A single int parameter called 'currentRandomIdleIndex' then determines which of these idles is currently playing.
    [SerializeField]
    public Motion[] randomIdleMotions;
    [SerializeField]
    public Motion runMotion;
    [SerializeField]
    public Motion jumpMotion;
    [SerializeField]
    public Motion runningJumpMotion;

	[SerializeField]
	public Motion crouchIdle;
	[SerializeField]
	public Motion crouchRun;

	[SerializeField]
	public Motion falling;
    
    [SerializeField]
    public AvatarMask upperBodyMask;
    [SerializeField]
    public Motion throwObject;

	[SerializeField]
	public float speedForRun = 5.0f;

	// This function generates idleMotions.Length number of idle states, naming them "RandomIdle_0"..."RandomIdle_N".
	public StateMachineNodeContext[] GetRandomIdleStates(Motion[] idleMotions)
	{
		Assert.IsTrue(idleMotions.Length > 0);
		StateMachineNodeContext[] result = new StateMachineNodeContext[idleMotions.Length];
		for(int i = 0; i < result.Length; ++i)
		{
			result[i] = State("RandomIdle_" + i.ToString(), idleMotions[i]);
		}
		return result;
	}

	// This function generates a transition between every idle state to all other idle states based on the currentRandomIdleIndex parameter.
	// In this case there is (idleMotions.Length^2  - idleMotions.Length) number of transitions. This probably isn't necessary, but this
	// shows off how you might use functions like this to save yourself a massive amount of copying states and transitions.
	public TransitionContext[] GetTransitionsBetweenRandomIdles(Motion[] idleMotions, ParameterData indexParam)
	{
		// A transition from every state to every other state except itself.
		int count = (idleMotions.Length * idleMotions.Length) - idleMotions.Length;
		TransitionContext[] result = new TransitionContext[count];

		int transitionIndex = 0;

		for(int i = 0; i < idleMotions.Length; ++i)
		{
			string thisState = "RandomIdle_" + i.ToString();
			for(int t = 0; t < idleMotions.Length; ++t)
			{
				if(i != t)
				{
					string otherState = "RandomIdle_" + t.ToString();

					result[transitionIndex] = Transition().Source(thisState).Destination(otherState)
					[
						Int(indexParam, t, ConditionMode.Equals)
					];
					++transitionIndex;
				}
			}
		}

		return result;
	}

	// This function will construct and return the animator data.
	public override AnimatorData Construct()
	{
		// Declare all parameters we'll need for this graph.
		ParameterData speed = FloatParam("Speed");
		ParameterData isGrounded = BoolParam("IsGrounded", true);
		ParameterData jumpTrigger = TriggerParam("JumpTrigger");
		ParameterData isCrouched = BoolParam("IsCrouched", false);
        ParameterData currentRandomIdleIndex = IntParam("CurrentRandomIdleIndex", 0);
		ParameterData throwTrigger = TriggerParam("ThrowTrigger");
		
		Animator
		(
			Graph()
			[
				// You can have any number of layers you want. Just keep adding them under Graph[]. The layer index
				// is based on the order that they appear in this section. Layers can also be assigned tags that will be
				// given to all child states and state machines unless overridden.
				Layer("Base")
                    .Tag("BASE_LAYER_TAG")
				[
					// State machines can be nested as many times as needed. I find this very useful for grouping together states for
					// transitions. For example we use 'OnGround' and 'CrouchableStates' state machines to mass generate certain transitions.
					// All states in 'OnGround' can transition to 'Falling', and all states in 'CrouchableStates' can transition to 'Crouched'.
					StateMachine("OnGround")
					[
						StateMachine("CrouchableStates")
						[
							State("Run", runMotion),

							// Because we define a tag here, this statemachine and all it's children will now be given
							// the 'RANDOM_IDLES_TAG' instead of the previously defined 'BASE_LAYER_TAG'. I find this very useful for
							// determining what kind of states we're in for gameplay purposes. For example, our character logic can
							// check if we're in any JumpState by checking for the 'JUMP_TAG' tag before performing an attack. This means
							// we don't have to hard code checks for individual states and the character logic can be further separated from
							// our animator logic.
							StateMachine("RandomIdles")
								.Tag("RANDOM_IDLES_TAG")
							[
								// GetRandomIdleStates returns an array of StateMachineNodeContexts so we can call this function here to generate all 
								// the states we need based off of 'randomIdleMotions'.
								GetRandomIdleStates(randomIdleMotions)
							]
						],
						StateMachine("Crouched").DefaultState("CrouchIdle")
						[
							State("CrouchIdle", crouchIdle),
							State("CrouchRun", crouchRun)
						]
					],

					State("Falling", falling),
					
					StateMachine("JumpStates").Tag("JUMP_TAG")
					[
						State("Jump", jumpMotion),
						State("RunningJump", runningJumpMotion)
					]
				],

				// Our second layer is used for animations on the upper body. This is determined by the 'upperBodyMask' avatar mask we
				// pass into it.
                Layer("UpperBody")
                    .BlendingMode(LayerBlendingMode.Override)
                    .Weight(1.0f)
                    .Timing(true)
                    .IKPass(true)
                    .AvatarMask(upperBodyMask)
				[
					// Pass in null here so we give full control to the base layer when we're not playing an animation on the upper body.
                    State("UpperBodyNone", null),
					State("Throw", throwObject)
				]
			],
            
            Transitions()
            [
#if true
				// When defining a transition, we can use SourceRecursive to add all states under a state machine
				// recursively. This transition says that all random idle states can transition to run if the conditions
				// are met. This saves a lot of copying transitions, since they only differ in their source state.
				Transition().SourceRecursive("RandomIdles").Destination("Run")
				[
					Float(speed, speedForRun, ConditionMode.Greater),
					Bool(isGrounded, true)
				],
				// Same thing but back again. Transitioning to RandomIdles will add a transition to the RandomIdles state machine.
				Transition().Source("Run").Destination("RandomIdles")
				[
                    Float(speed, speedForRun, ConditionMode.Less),
					Bool(isGrounded, true)
				],

				// Crouched run.
				Transition().SourceRecursive("CrouchIdle").Destination("CrouchRun")
				[
					Float(speed, speedForRun, ConditionMode.Greater),
					Bool(isGrounded, true)
				],
				Transition().Source("CrouchRun").Destination("CrouchIdle")
				[
                    Float(speed, speedForRun, ConditionMode.Less),
					Bool(isGrounded, true)
				],
            
                // Here we use the OnGround state machine to easily define transitions for everything that can be considered on ground.
				// Don't want a particular animation to transition to jump? That's easy, just don't put it in the 'OnGround' state machine.
				Transition().SourceRecursive("OnGround").Destination("Jump")
				[
					Trigger(jumpTrigger),
					Float(speed, speedForRun, ConditionMode.Less)
				],
				Transition().SourceRecursive("OnGround").Destination("RunningJump")
				[
					Trigger(jumpTrigger),
					Float(speed, speedForRun, ConditionMode.Greater)
				],
				// We can use SourceMultiple to define a list of specific states as sources. This is how you define a transition with an exit time.
				// Without specifying an exit time, the transition is assume to have none.
				Transition().SourceMultiple("Jump", "RunningJump").Destination("RandomIdles").ExitTime(0.9f),

				// Same as above, we're using CrouchableStates to logically define which states can actually go into crounch.
				// We use 'Crouched' statemachine to say which states can transition back.
				Transition().SourceRecursive("CrouchableStates").Destination("Crouched")
				[
					Bool(isCrouched, true)
				],
				Transition().SourceRecursive("Crouched").Destination("CrouchableStates")
				[
					Bool(isCrouched, false)
				],

				// Falling
				Transition().SourceRecursive("OnGround").Destination("Falling")
				[
					Bool(isGrounded, false)
				],
				Transition().Source("Falling").Destination("OnGround")
				[
					Bool(isGrounded, true)
				],

				// Throwing using upper body layer
				Transition().Source("UpperBodyNone").Destination("Throw")
				[
					Trigger(throwTrigger)
				],
				Transition().Source("Throw").Destination("UpperBodyNone").DefaultExitTime(),
#else
				// The above transition definitions can also be expressed much more concisely using this form. In a way this is less
				// readable, and also gives you less options. I personally prefer this way in general, though.
				Transition(Recursive("RandomIdles"), "Run", Float(speed, speedForRun, ConditionMode.Greater), Bool(isGrounded, true)),
				Transition(Single("Run"), "RandomIdles", Float(speed, speedForRun, ConditionMode.Less), Bool(isGrounded, true)),
				
				Transition(Recursive("CrouchIdle"), "CrouchRun", Float(speed, speedForRun, ConditionMode.Greater), Bool(isGrounded, true)),
				Transition(Single("CrouchRun"), "CrouchIdle", Float(speed, speedForRun, ConditionMode.Less), Bool(isGrounded, true)),
            
				Transition(Recursive("OnGround"), "Jump", Trigger(jumpTrigger), Float(speed, speedForRun, ConditionMode.Less)),
				Transition(Recursive("OnGround"), "RunningJump", Trigger(jumpTrigger), Float(speed, speedForRun, ConditionMode.Greater)),
				Transition(Multiple("Jump", "RunningJump"), "RandomIdles", 0.9f),
				
				Transition(Recursive("CrouchableStates"), "Crouched", Bool(isCrouched, true)),
				Transition(Recursive("Crouched"), "CrouchableStates", Bool(isCrouched, false)),
				
				Transition(Recursive("OnGround"), "Falling", Bool(isGrounded, false)),
				Transition(Single("Falling"), "OnGround", Bool(isGrounded, true)),

				Transition(Single("UpperBodyNone"), "Throw", Trigger(throwTrigger)),
				Transition(Single("Throw"), "UpperBodyNone", DefaultExitTime),
#endif

				// Since 'GetTransitionsBetweenRandomIdles' return an array of TransitionContext, we can call it here to
				// perform more complex logic to generate the random idle transitions for us.
				GetTransitionsBetweenRandomIdles(randomIdleMotions, currentRandomIdleIndex)
            ]
		);
		
        return currentData;
	}
}

// ==================================================================
// ======================= Blend Tree Test ==========================
// ==================================================================
[System.Serializable]
public class LeftRightForwardMotionSet
{
	// This class is used to define a set of animations for various turn rates. This way we don't have to duplicate
	// all the code to define walk states vs run states.
	[SerializeField]
	public float speedThreshold;

	[SerializeField]
	public Motion sharpLeft;
	[SerializeField]
	public Motion left;
	[SerializeField]
	public Motion forward;
	[SerializeField]
	public Motion right;
	[SerializeField]
	public Motion sharpRight;
}
public class ClassBlendTreeTest : CSharpAnimator_ClassBase
{
	// As mentioned before, you can serialized anything here, including custom classes. We do this to easily define
	// walk and run turning animations.
    [SerializeField]
    public Motion idle;
	[SerializeField]
	public LeftRightForwardMotionSet walk;
	[SerializeField]
	public LeftRightForwardMotionSet run;

	// This function will generate a blend motion for a given set of turning animations.
	public MotionContext GenerateLeftRightForwardBlendMotion(ParameterData param, LeftRightForwardMotionSet set)
	{
		MotionContext result = 
		BlendMotion1D(param, false)
		[
			BlendClip1D(set.sharpLeft, 0.0f),
			BlendClip1D(set.left, 2.0f)
		];

		// You can add additional inputs like this:
		result.AddBlendMotions(BlendClip1D(set.forward, 5.0f), BlendClip1D(set.right, 8.0f));
		
		// Or you can use [] operator again:
		result = result
		[
			BlendClip1D(set.sharpRight, 10.0f)
		];
		
		return result;
	}
	
	public override AnimatorData Construct()
	{
		// Our blend parameters are define like usual parameters.
		ParameterData speed = FloatParam("Speed", 0.0f);
		ParameterData turn = FloatParam("Turn", 0.5f);

		Animator
		(
			Graph()
			[
				Layer("Base").DefaultState("LocomotionBlend")
				[
					// First we define a blend state. This is just like any other state, except inside it's []
					// brackets, it takes a single motion. This can be:
					// - BlendMotion1D
					// - BlendMotion2DSimpleDirectional
					// - BlendMotion2DFreeFormDirectional
					// - BlendMotion2DFreeFormCartesian
					// - BlendMotionDirect
					// - BlendClip1D
					// - BlendClip2D
					// - BlendClipDirect
					// All of the above can take any number of the above inside it's [] brackets recursively. Using
					// this we can construct blend trees. This is an example of idle, walk, and run animations being blended
					// together based on speed and turn rate. As you can see we can also define the blend thresholds as an
					// exposed parameter, so they don't have to be hard coded here. The only draw back is, we don't get a
					// nice animation preview as we tweak the values.
					BlendState("LocomotionBlend")
					[
						BlendMotion1D(speed, false)
						[
							BlendClip1D(idle, 0.0f),
							GenerateLeftRightForwardBlendMotion(turn, walk).ThresholdX(walk.speedThreshold),
							GenerateLeftRightForwardBlendMotion(turn, run).ThresholdX(run.speedThreshold)
						]
					]
				]
			],
			
			// Having no transitions is okay.
			Transitions()
		);
		
		return currentData;
	}
}
#endif