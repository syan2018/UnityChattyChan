using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimationCreator
{
    [MenuItem("Assets/Create Animations From Selected Images")]
    private static void CreateAnimationsFromSelectedImages()
    {
        // 获取选中的对象
        Object[] selectedObjects = Selection.objects;

        foreach (Object selectedObject in selectedObjects)
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);

            if (Path.GetExtension(assetPath) == ".png")  // 仅处理PNG文件
            {
                string outputDirectory = Path.GetDirectoryName(assetPath);  // 获取源图片的目录
                string animationPath = Path.Combine(outputDirectory, assetName + ".anim");  // 创建动画文件的路径

                // 创建动画剪辑
                AnimationClip animationClip = new AnimationClip();
                animationClip.name = assetName;
                animationClip.frameRate = 1;  // 设置帧率为1，使每个动画只有一帧

                // 创建动画编辑器曲线绑定和动画编辑器曲线
                EditorCurveBinding curveBinding = new EditorCurveBinding();
                curveBinding.type = typeof(SpriteRenderer);
                curveBinding.path = "";
                curveBinding.propertyName = "m_Sprite";

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);  // 加载图片作为精灵
                ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe();
                keyframe.time = 0;
                keyframe.value = sprite;

                // 将曲线添加到动画剪辑
                AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, new ObjectReferenceKeyframe[] { keyframe });

                // 保存动画剪辑到磁盘
                AssetDatabase.CreateAsset(animationClip, animationPath);
            }
        }

        // 刷新资产数据库以显示新创建的动画
        AssetDatabase.Refresh();
    }

    // 确保菜单项只在选中了至少一个对象时启用
    [MenuItem("Assets/Create Animations From Selected Images", true)]
    private static bool ValidateCreateAnimationsFromSelectedImages()
    {
        return Selection.objects.Length > 0;
    }
}

