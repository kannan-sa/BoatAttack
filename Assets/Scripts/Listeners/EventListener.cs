using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Events.Listeners
{
    public class EventListener : MonoBehaviour
    {
       
    }

    public abstract class EventListener<T> : MonoBehaviour
    {
        public bool Log;
        public bool InitializeOnStart;

        [SerializeField]
        protected GameEvent<T> eventObject;

        [SerializeField] 
        protected UnityEvent<T> OnEvent;

        private T preValue;

        private void OnEnable()
        {
            //Debug.Log(name);
            preValue = (T)eventObject;
            eventObject.AddListener(onValueChange);
        }

        private void Start()
        {
            if(InitializeOnStart) 
                OnEvent.Invoke(preValue);
        }

        private void OnDisable()
        {
            eventObject.RemoveListener(onValueChange);
        }

        private void onValueChange(T value) {
            OnValueChange(value);
        }

        protected virtual bool OnValueChange(T value) {
            bool result = preValue == null || !preValue.Equals(value);

            if (Log)
                Debug.Log($" on {name} condition is {result}, value is {value}", gameObject);

            if (result)
                OnEvent.Invoke(preValue = value);
            return result;
        }
    }
}
