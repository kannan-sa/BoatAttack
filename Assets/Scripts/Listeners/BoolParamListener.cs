using UnityEngine;

namespace Events.Listeners {
    public sealed class BoolParamListener : EventListener<bool> {
        [SerializeField]
        private string parameter;
        [SerializeField]
        private string trigger;
        [SerializeField]
        private Animator animator;

        protected override bool OnValueChange(bool value) {
            bool result = base.OnValueChange(value);
            if (result) {
                animator.SetBool(parameter, value);
                if (!string.IsNullOrEmpty(trigger))
                    animator.SetTrigger(trigger);
            }
            return result;
        }
    }
}