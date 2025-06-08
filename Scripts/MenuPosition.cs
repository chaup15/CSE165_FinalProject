using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float visibleAngleThreshold = 60f; // 菜单面向相机时的最大角度
    GameObject wristMenu = null;
    // Start is called before the first frame update
    // 示例：绑定 WristMenu 到左手

    void Start()
    {
        wristMenu = GameObject.Find("WristMenu");

        // 获取左手的 Transform（一般通过 XR Rig 的 LeftHand Controller）
        var leftHand = GameObject.Find("[BuildingBlock] Hand Tracking left").transform;

        // 将菜单设为左手子物体
        wristMenu.transform.SetParent(leftHand);

        // 调整本地位置和旋转
        wristMenu.transform.localPosition = new Vector3(-0.1f, 0.025f, 0.0f); // 调整为手腕合适的位置
        wristMenu.transform.localRotation = Quaternion.Euler(90, 90, 0);   // 让按钮朝上
    }


    // Update is called once per frame
    void Update()
    {
        if (wristMenu == null) return;

        // 计算菜单是否面朝摄像头
        Vector3 toCamera = Camera.main.transform.position - wristMenu.transform.position;
        float angle = Vector3.Angle(wristMenu.transform.forward, toCamera);

        // 控制 Canvas 显示
        wristMenu.SetActive(angle > visibleAngleThreshold);
        // Debug.Log($"Menu visibility angle: {angle} degrees, Active: {wristMenu.activeSelf}");
    }
}