using UnityEngine;

namespace Lumb
{
    /// <summary>
    /// 相机不动模型动
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ModelDisplay : MonoBehaviour
    {
        #region Inspector
        [Header("当前控制模型")]
        [SerializeField]
        private GameObject currentCtrlGo;
        
        [Header("旋转控制")]
        public MouseKey rotationKey=MouseKey.RightMouse;

        [Range(0, 20f)] public float rotationSpeed = 8f;

        public Vector2 xRotationClamp = new Vector2(-80f, 80f);
        public Vector2 yRotationClamp = new Vector2(-80f, 80f);

        [Header("拖拽控制")]
        public MouseKey moveKey=MouseKey.MiddleMouse;
        [Range(0, 10f)]
        public float moveSpeed = 5f;
        public bool isEndRecoveryPos;
        [Header("大小控制")]
        public bool isCtrlScale;
        [Range(0,10f)]
        public float minScale=0.1f;
        [Range(0,10f)]
        public float maxScale=3f;
        #endregion

        private CtrlModelState ctrlModelState;

        Vector3 ctrlGoDefaultPos;
        Quaternion ctrlGoDefaultRot;
        Vector3 ctrlGoDefaultScale;

        Transform tempParent;
        Transform oldParent;

        Vector3 moveStartTempPos;

        Vector3 mouseStartPos;
        Vector3 moveStartSceenPos;

        float mouse_x ;
        float mouse_y ;

        float scroll;



        public void Start()
        {
            ctrlModelState = CtrlModelState.Free;
            tempParent = new GameObject("tempParent").transform;

            UpdateRecoveryValue();

        }

        void Update()
        {
            if(currentCtrlGo==null||tempParent==null||!currentCtrlGo.activeSelf)
                return;

            GetMouseInput();
            //旋转
            Rotate();

            //拖拽
            Drag();

            //缩放
            Zoom();
        }
        /// <summary>
        /// 拖拽
        /// </summary>
        protected virtual void Drag()
        {
            if (ctrlModelState == CtrlModelState.Free && moveKey != MouseKey.None && Input.GetMouseButtonDown((int)moveKey))
            {
                ctrlModelState = CtrlModelState.Move;

                mouseStartPos = Input.mousePosition;

                moveStartTempPos = tempParent.position;

                currentCtrlGo.transform.SetParent(tempParent);

                moveStartSceenPos = GetComponent<Camera>().WorldToScreenPoint(tempParent.position);
            }
            else if (ctrlModelState == CtrlModelState.Move && moveKey != MouseKey.None && Input.GetMouseButton((int)moveKey))
            {
                Vector3 newPos = moveStartSceenPos + Input.mousePosition - mouseStartPos;

                Vector3 offset = GetComponent<Camera>().ScreenToWorldPoint(newPos);

                tempParent.position = offset;
            }
            else if (ctrlModelState == CtrlModelState.Move && moveKey != MouseKey.None && Input.GetMouseButtonUp((int)moveKey))
            {
                ctrlModelState = CtrlModelState.Free;

                if (isEndRecoveryPos)
                {
                    tempParent.position = moveStartTempPos;
                }

                currentCtrlGo.transform.SetParent(oldParent);
            }
        }
        /// <summary>
        /// 缩放
        /// </summary>
        protected virtual void Zoom()
        {
            if(isCtrlScale&& scroll != 0)
            {
                ctrlModelState = CtrlModelState.Salce;
                float currentScale = tempParent.localScale.x;
                if ((scroll > 0 && currentScale+scroll >= maxScale) || (scroll < 0 && currentScale+scroll < minScale))
                    return;
                currentCtrlGo.transform.SetParent(tempParent);
                tempParent.transform.localScale = new Vector3(tempParent.localScale.x + scroll,
                    tempParent.localScale.x + scroll,
                    tempParent.localScale.x + scroll);
                currentCtrlGo.transform.SetParent(oldParent);
            }
            else if (ctrlModelState == CtrlModelState.Salce)
            {
                currentCtrlGo.transform.SetParent(oldParent);
                ctrlModelState = CtrlModelState.Free;
            }
        }

        /// <summary>
        /// 旋转
        /// </summary>
        protected virtual void Rotate()
        {
            //旋转
            if (ctrlModelState == CtrlModelState.Free && Input.GetMouseButtonDown(((int)rotationKey)))
            {
                ctrlModelState = CtrlModelState.Rotation;


                tempParent.LookAt(transform);

                currentCtrlGo.transform.SetParent(tempParent);
            }
            else if (ctrlModelState == CtrlModelState.Rotation && Input.GetMouseButton((int)rotationKey))
            {
                //targetRotation.x += mouse_x;
                //targetRotation.x += mouse_y * rotationSpeed;
                //targetRotation.y -= mouse_x * rotationSpeed;

                //targetRotation.x = Mathf.Clamp(targetRotation.x, xRotationClamp.x, xRotationClamp.y);
                //targetRotation.y = Mathf.Clamp(targetRotation.y, yRotationClamp.x, yRotationClamp.y);
                //currentCtrlGo.transform.eulerAngles = targetRotation;
                Vector3 vp = new Vector3(-mouse_y, -mouse_x, 0) * rotationSpeed;
                tempParent.Rotate(vp, Space.Self);
            }
            else if (ctrlModelState == CtrlModelState.Rotation && Input.GetMouseButtonUp((int)rotationKey))
            {
                ctrlModelState = CtrlModelState.Free;
                currentCtrlGo.transform.SetParent(oldParent);
            }
        }
        /// <summary>
        /// 获得鼠标输入
        /// </summary>
        void GetMouseInput()
        {
            mouse_x = Input.GetAxis("Mouse X");
            mouse_y = Input.GetAxis("Mouse Y");

            scroll = Input.GetAxis("Mouse ScrollWheel");
        }

        /// <summary>
        /// 设置当前操控的物体
        /// </summary>
        public void SetCurrentCtrlGo(GameObject go)
        {
            RecoveryCtrModel();
            if (go == currentCtrlGo)
                return;
            currentCtrlGo.SetActive(false);
            currentCtrlGo = go;
            UpdateRecoveryValue();
        }
        /// <summary>
        /// 重置模型状态
        /// </summary>
        public void RecoveryCtrModel()
        {
            if (currentCtrlGo != null)
            {
                currentCtrlGo.transform.position = ctrlGoDefaultPos;
                currentCtrlGo.transform.rotation = ctrlGoDefaultRot;
                currentCtrlGo.transform.localScale = ctrlGoDefaultScale;

                currentCtrlGo.transform.SetParent(oldParent);

                tempParent.position = GetCenter(currentCtrlGo);
            }
        }

        Vector3 GetCenter(GameObject target)
        {
            Renderer[] renderers = target.gameObject.GetComponentsInChildren<Renderer>();

            Vector3 center = target.transform.position;

            if(renderers.Length!=0)
            {
                Bounds bounds = new Bounds(renderers[0].transform.position, Vector3.zero);

                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                center = bounds.center;
            }

            return center;
        }

        /// <summary>
        /// 更新模型的初始参数
        /// </summary>
        void UpdateRecoveryValue()
        {
            if (currentCtrlGo != null)
            {
                tempParent.position = GetCenter(currentCtrlGo);

                ctrlGoDefaultPos = currentCtrlGo.transform.position;
                ctrlGoDefaultRot = currentCtrlGo.transform.rotation;
                ctrlGoDefaultScale = currentCtrlGo.transform.localScale;

                oldParent = currentCtrlGo.transform.parent;
            }
        }
    }

    public enum MouseKey
    {
        
        LeftMouse=0,
        RightMouse=1,
        MiddleMouse =2,
        None
    }

    public enum CtrlModelState
    {
        Free,
        Rotation,
        Move,
        Salce
    }
}

