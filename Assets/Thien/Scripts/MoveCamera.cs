using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyTrak.CoursePlay
{
    public class MoveCamera : MonoBehaviour
    {
        public float TurnSpeed = 4.0f;      // Speed of camera turning when mouse moves in along an axis
        public float MoveSpeed = 5.0f;      // Speed of the camera going back and forth
        public float sensitivity = 0.1f;
        public float minAngleX = -15f;
        public float maxAngleX = 15f;
        public float minAngleY = -8f;
        public float maxAngleY = 15f;
        
        public Camera cam;

        private Vector3 _oldPosition;
        private Vector3 _move;
        
        void Update()
        {
            _oldPosition = transform.position;
            
            TranslateCamera();

            RotateCamera();

            ZoomCamera();
        }

        private void TranslateCamera()
        {
            TranslateByScrollWheel();
            
            TranslateByKeyCode();
        }

        private void TranslateByScrollWheel()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            
            if (!Input.GetMouseButton(2)) return;
            
            _move = Vector3.zero;
            _move += (Input.GetAxis("Mouse X") * MoveSpeed) * Vector3.left;
            _move += (Input.GetAxis("Mouse Y") * MoveSpeed) * Vector3.down;

            if (Input.GetKey(KeyCode.LeftShift)) _move *= 3;
                
            transform.Translate(_move, Space.Self);
        }

        private void TranslateByKeyCode()
        {
            _move = Vector3.zero;
            var speed = 0.4f * MoveSpeed;
            
            if ((Input.GetKey(KeyCode.A) && !IsSelectInputField()) || Input.GetKey(KeyCode.LeftArrow))
            {
                _move += speed * Vector3.left;
            }
            if ((Input.GetKey(KeyCode.D) && !IsSelectInputField()) || Input.GetKey(KeyCode.RightArrow))
            {
                _move += speed * Vector3.right;
            }
            if ((Input.GetKey(KeyCode.W) && !IsSelectInputField()) || Input.GetKey(KeyCode.UpArrow))
            {
                _move += speed * Vector3.forward;
            }
            if ((Input.GetKey(KeyCode.S) && !IsSelectInputField()) || Input.GetKey(KeyCode.DownArrow))
            {
                _move += speed * Vector3.back;
            }
            
            if (Input.GetKey(KeyCode.LeftShift)) _move *= 3;
            
            transform.Translate(_move, Space.Self);
        }
        
        private void RotateCamera()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            
            if (!Input.GetMouseButton(1)) return;
            
            transform.eulerAngles += new Vector3(TurnSpeed * -Input.GetAxis("Mouse Y"), TurnSpeed * Input.GetAxis("Mouse X"), 0.0f);
        }
        
        public LayerMask layerToHit;
        // ReSharper disable Unity.PerformanceAnalysis
        private void ZoomCamera()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            
            var scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheel) <= Mathf.Epsilon) return;
            
            transform.position = Vector3.LerpUnclamped(transform.position, PlanePosition(), scrollWheel * sensitivity);
        }
        
        private Ray _ray;
        private RaycastHit[] _hitData = new RaycastHit[1];
        private Vector3 PlanePosition()
        {
            _ray = cam.ScreenPointToRay(Input.mousePosition);
            var hitLength = Physics.RaycastNonAlloc(_ray, _hitData, Mathf.Infinity, layerToHit);
            
            return hitLength == 0 ? Vector3.zero : _hitData[0].point;
        }

        private bool _isDirty = true;
        private bool _isSelectInputField;
        private GameObject _inputFieldGo;
        private InputField _inputField;
        private TMP_InputField _tmpInputField;
        private bool IsSelectInputField()
        {
            if (!_isDirty) return _isSelectInputField;

            _isDirty = false;
            DelayCB(() => _isDirty = true, 100);
            
            _inputFieldGo = EventSystem.current.currentSelectedGameObject;
            if (_inputFieldGo == null)
            {
                _isSelectInputField = false;
                return _isSelectInputField;
            }
            
            _tmpInputField = _inputFieldGo.GetComponent<TMP_InputField>();
            if (_tmpInputField != null)
            {
                _isSelectInputField = true;
                return _isSelectInputField;
            }
            
            _inputField = _inputFieldGo.GetComponent<InputField>();
            if (_inputField != null)
            {
                _isSelectInputField = true;
                return _isSelectInputField;
            }

            _isSelectInputField = false;
            return _isSelectInputField;
        }

        private async void DelayCB(Action cb, int delayTime)
        {
            await Task.Delay(delayTime);
            
            cb?.Invoke();
        }
    }
}