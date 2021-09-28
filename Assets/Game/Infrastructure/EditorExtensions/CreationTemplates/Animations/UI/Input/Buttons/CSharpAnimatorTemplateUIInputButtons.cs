using System.Globalization;
using Infrastructure.Constants;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.EditorExtensions.CreationTemplates.Animations.UI.Input.Buttons
{
    public class CSharpAnimatorTemplateUIInputButtons : CSharpAnimator
    {
        [SerializeField] public float pressedTime; 
        [SerializeField] public Motion defaultMotion;
        [SerializeField] public Motion clickMotion;
        
        private readonly string DEFAULT_STATE = UIConstants.DEFAULT_STATE;
        private readonly string CLICK_STATE = UIConstants.CLICK_STATE;
        
        [MenuItem("Assets/Create/CSharpAnimator/Button input ui animation controller")]
        public static void Create()
        {
            CreateWithType(typeof(CSharpAnimatorTemplateUIInputButtons));
        }

        public override AnimatorData Construct()
        {
            ParameterData click = TriggerParam(CLICK_STATE);
            
            Animator(
                Graph()
                [
                    Layer("Base").DefaultState(DEFAULT_STATE)
                    [
                        State(DEFAULT_STATE, defaultMotion),
                        State(CLICK_STATE, clickMotion)
                    ]
                ],
                Transitions()
                [
                    Transition().Source(DEFAULT_STATE).Destination(CLICK_STATE)[Trigger(click)],
                    Transition().Source(CLICK_STATE).Destination(DEFAULT_STATE).ExitTime(pressedTime)
                ]
            );            
            
            return currentData;
        }
    }
}


