using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using UnityEngine.Android;

namespace Elxsi
{
    public class GamepadReader
    {
        public static bool inReading;

        private int index;
        private InputControls inputAction;

        public GamepadReader(int index, Gamepad gamepad)
        {
            this.index = index;
            inputAction = new InputControls();
            inputAction.BoatControls.Steering.Enable();
            inputAction.BoatControls.Steering.performed += OnSteeringPerformed;
            inputAction.BoatControls.Steering.canceled += OnSteeringCancelled;

            inputAction.BoatControls.Trottle.Enable();
            inputAction.BoatControls.Trottle.performed += OnTrottlePerformed;
            inputAction.BoatControls.Trottle.canceled += OnTrottleCancelled;
        }

        private void OnSteeringPerformed(InputAction.CallbackContext context)
        {
            Gamepads.inputData[index] = context.ReadValue<float>();
            Gamepads.needsWriting = inReading = true;
        }
        private void OnSteeringCancelled(InputAction.CallbackContext context)
        {
            Gamepads.inputData[index] = context.ReadValue<float>();
            inReading = false;
        }

        private void OnTrottlePerformed(InputAction.CallbackContext context)
        {
            Gamepads.inputData[index + 3] = context.ReadValue<float>();
            Gamepads.needsWriting = inReading = true;
        }

        private void OnTrottleCancelled(InputAction.CallbackContext context)
        {
            Gamepads.inputData[index + 3] = context.ReadValue<float>();
            inReading = false;
        }

        ~GamepadReader()
        {
            inputAction.BoatControls.Steering.performed -= OnSteeringPerformed;
            inputAction.BoatControls.Steering.canceled -= OnSteeringCancelled;
            inputAction.BoatControls.Steering.Disable();

            inputAction.BoatControls.Trottle.performed -= OnTrottlePerformed;
            inputAction.BoatControls.Trottle.canceled -= OnTrottleCancelled;
            inputAction.BoatControls.Trottle.Disable();
            inputAction.Dispose();
        }
    }

    public class Gamepads : MonoBehaviour
    {
#if UNITY_EDITOR
        private string filePath = "D:/Downloads/BoatAttack/Gamepads.bin";
#else
        private string filePath = "/storage/emulated/10/Documents/Gamepads.bin";
#endif

        public Text log;
        public Toggle logToggle;

        public static List<float> inputData = new List<float>() { 0f, 0f, 0f, 0f, 0, 0f };

        private bool isFocusing;

        private int count = 1;
        private int maxCount = 2;

        FileStream fileStream = null;
        BinaryReader reader = null;
        BinaryWriter writer = null;

        public static bool needsWriting = false;

        private List<GamepadReader> readers = new List<GamepadReader>();

        private string status;

        private void OnEnable()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) ||
                !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }


            try
            {
                string dirPath = Path.GetDirectoryName(filePath);
                // Ensure the directory exists
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
                reader = new BinaryReader(fileStream);
                writer = new BinaryWriter(fileStream);

                for (int i = 0; i < Gamepad.all.Count; i++)
                    readers.Add(new GamepadReader(i, Gamepad.all[i]));
            }
            catch (Exception e)
            {
                log.text = e.Message;
                logToggle.isOn = true;
            }
        }

        private void OnDisable()
        {
            reader?.Close();
            reader?.Dispose();
            writer?.Close();
            writer?.Dispose();
            fileStream?.Close();
            fileStream?.Dispose();

            readers.Clear();
        }

        private void OnApplicationFocus(bool focus)
        {
            isFocusing = focus;
        }

        private void LateUpdate()
        {
            if (reader == null || writer == null)
                return;

            status = string.Empty;
            count++;

            if (count % maxCount != 0)
                return;

            if (!isFocusing) //Read..
                ReadFloatArrayFromFile();
            else //if (needsWriting)   //Write..
                WriteFloatArrayToFile();

            log.text = $"C = {count}, S = {status} F = {isFocusing} ,  LX = {inputData[0]}";
        }

        public void WriteFloatArrayToFile()
        {
            status = "W";
            writer.Seek(0, SeekOrigin.Begin);
            foreach (float value in inputData)
                writer.Write(value);
            needsWriting = GamepadReader.inReading;
        }

        public void ReadFloatArrayFromFile()
        {
            status = "R";

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            int count = 0;
            while (fileStream.Position < fileStream.Length && count < inputData.Count)
                inputData[count++] = reader.ReadSingle();
        }
    }
}