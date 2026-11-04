# Stage 4A: isolated AR foundation

SQLJoin_AR_Android.asset overrides only its scene list. It does NOT override Graphics or Quality settings and is not activated automatically.

SQLJoin_AR_Pipeline.asset points to SQLJoin_AR_Renderer.asset, which contains ARBackgroundRendererFeature and ARCommandBufferSupportRendererFeature. Depth priming is disabled. The existing mobile pipeline and global graphics/quality settings are unchanged.

Before camera-background testing, configure and verify profile-specific Graphics/Quality overrides in Unity 6.6 so the effective pipeline is SQLJoin_AR_Pipeline. A quality-level pipeline assignment takes precedence over a Graphics default. Do not assign the pipeline in global Project Settings. If profile-local pipeline overrides cannot be configured, stop and plan a separate scoped selection mechanism before device testing.

The camera renderer index is -1 (use the effective pipeline default). No camera can select a renderer asset that is absent from that effective pipeline.

Android currently uses Automatic Graphics APIs, with Vulkan and OpenGLES3 in its serialized list. Installed AR Foundation 6.6 documentation requires ARBackgroundRendererFeature and lists command-buffer support as optional for ARCore/Vulkan; both are included in this isolated renderer.

The scene intentionally has no visualization, placement script, anchor instance, plane visualization prefab, or active JOIN UI. ARAnchorManager is only a service. SQLJoinController is a disabled component on SQLJoin_AR_Canvas.

Manual checks: import assets; inspect missing scripts/references; confirm horizontal detection and one enabled AR camera; inspect feature list and effective pipeline; validate Android XR; activate only the new profile for a dedicated build; verify camera permission/background and tracking on device. No device or Editor runtime verification has been performed by this stage.
