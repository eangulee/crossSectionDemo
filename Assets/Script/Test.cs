using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static Planar_xyzClippingSection;

[RequireComponent(typeof(CrossSectionObjectSetup))]
public class Test : MonoBehaviour
{
    CrossSectionObjectSetup cs_setup;
    public Slider slider;

    //public enum ConstrainedAxis { X, Y, Z };
    public ConstrainedAxis selectedAxis = ConstrainedAxis.X;    

    private GameObject rectGizmo;
    private Vector3 sectionplane = Vector3.up;
    private Vector3 zeroAlignmentVector = Vector3.zero;

    private float sectionX = 0;
    private float sectionY = 10;
    private float sectionZ = 0;

    void Awake()
    {
        cs_setup = gameObject.GetComponent<CrossSectionObjectSetup>();
    }

    void Start()
    {
        if (slider) slider.onValueChanged.AddListener(SliderListener);
        Shader.DisableKeyword("CLIP_TWO_PLANES");
        Shader.EnableKeyword("CLIP_PLANE");

        switch (selectedAxis)
        {
            case ConstrainedAxis.X:
                sectionplane = Vector3.right;
                break;
            case ConstrainedAxis.Y:
                sectionplane = Vector3.up;
                break;
            case ConstrainedAxis.Z:
                sectionplane = Vector3.forward;
                break;
            default:
                Debug.Log("case default");
                break;
        }

        Shader.SetGlobalVector("_SectionPlane", sectionplane);

        zeroAlignmentVector = new Vector3(0, 1, 0);

        InitGizmo();
        UpdateSection();
    }

    public void SliderListener(float value)
    {
        switch (selectedAxis)
        {
            case ConstrainedAxis.X:
                sectionX = value + zeroAlignmentVector.x;
                if (rectGizmo) rectGizmo.transform.position = new Vector3(sectionX, cs_setup.bounds.center.y, cs_setup.bounds.center.z);
                break;
            case ConstrainedAxis.Y:
                sectionY = value + zeroAlignmentVector.y;
                if (rectGizmo) rectGizmo.transform.position = new Vector3(cs_setup.bounds.center.x, sectionY, cs_setup.bounds.center.z);
                break;
            case ConstrainedAxis.Z:
                sectionZ = value + zeroAlignmentVector.z;
                if (rectGizmo) rectGizmo.transform.position = new Vector3(cs_setup.bounds.center.x, cs_setup.bounds.center.y, sectionZ);
                break;
        }
        Shader.SetGlobalVector("_SectionPoint", new Vector3(sectionX, sectionY, sectionZ));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Collider coll = gameObject.GetComponent<Collider>();
            if (coll.Raycast(ray, out hit, 10000f))
            {
                StartCoroutine(dragGizmo());
            }
        }
    }

    IEnumerator dragGizmo()
    {
        //基本思路：将鼠标在屏幕上的移动距离换算为空间距离，再分别投影到X\Y\Z轴

        //空间距离
        float cameraDistance = Vector3.Distance(cs_setup.bounds.center, Camera.main.transform.position);

        //记录初始化位置
        Vector3 startPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
        Vector3 startPos = rectGizmo.transform.position;

        Vector3 translation = Vector3.zero;
        //Camera.main.GetComponent<maxCamera>().enabled = false;

        if (slider) slider.onValueChanged.RemoveListener(SliderListener);

        while (Input.GetMouseButton(0))
        {
            //换算移动距离
            translation = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance)) - startPoint;

            //分别投影到X\Y\Z轴
            Vector3 projectedTranslation = Vector3.Project(translation, sectionplane);

            Vector3 newPoint = startPos + projectedTranslation;
            switch (selectedAxis)
            {
                case ConstrainedAxis.X:
                    if (newPoint.x > cs_setup.bounds.max.x) sectionX = cs_setup.bounds.max.x;
                    else if (newPoint.x < cs_setup.bounds.min.x) sectionX = cs_setup.bounds.min.x;
                    else sectionX = newPoint.x;
                    break;
                case ConstrainedAxis.Y:
                    if (newPoint.y > cs_setup.bounds.max.y) sectionY = cs_setup.bounds.max.y;
                    else if (newPoint.y < cs_setup.bounds.min.y) sectionY = cs_setup.bounds.min.y;
                    else sectionY = newPoint.y;
                    break;
                case ConstrainedAxis.Z:
                    if (newPoint.z > cs_setup.bounds.max.z) sectionZ = cs_setup.bounds.max.z;
                    else if (newPoint.z < cs_setup.bounds.min.z) sectionZ = cs_setup.bounds.min.z;
                    else sectionZ = newPoint.z;
                    break;
            }
            UpdateSection();
            yield return null;
        }
        //Camera.main.GetComponent<maxCamera>().enabled = true;
        if (slider)
            slider.onValueChanged.AddListener(SliderListener);
    }

    //刷新断面位置
    void UpdateSection()
    {
        Vector3 sectionpoint = new Vector3(sectionX, sectionY, sectionZ);

        Shader.SetGlobalVector("_SectionPoint", sectionpoint);
        if (rectGizmo)
            rectGizmo.transform.position = new Vector3(cs_setup.bounds.center.x, sectionY, cs_setup.bounds.center.z);

        //更新进度条
        switch (selectedAxis)
        {
            case ConstrainedAxis.X:
                slider.value = sectionX;
                break;
            case ConstrainedAxis.Y:
                slider.value = sectionY;
                break;
            case ConstrainedAxis.Z:
                slider.value = sectionZ;
                break;
            default:
                Debug.Log("case default");
                break;
        }

    }

    //初始化示意框
    void InitGizmo()
    {
        rectGizmo = Resources.Load("rectGizmo") as GameObject;
        rectGizmo = Instantiate(rectGizmo, cs_setup.bounds.center + (-cs_setup.bounds.extents.y + (slider ? slider.value : 0) + zeroAlignmentVector.y) * transform.up, Quaternion.identity) as GameObject;
        RectGizmo rg = rectGizmo.GetComponent<RectGizmo>();
        rg.SetSizedGizmo(cs_setup.bounds.size, selectedAxis);
    }
}