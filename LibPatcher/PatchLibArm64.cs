using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using LibPatcher;
using System.Security.Cryptography;
using System.Text;

internal class PatchLibArm64 : BasePatchLib
{
    // Private constructor to initialize the base class with Arm64 platform
    private PatchLibArm64() : base(PlatformEnum.Arm64)
    {
    }

    // Static method to start the patching process
    internal static void Start()
    {
        new PatchLibArm64();
    }

    // Override method to patch MethodAccessException
    protected override PatchData Patch_MethodAccessException()
    {
        return new PatchData("mono_method_can_access_method", 0x24 + 0x1c,
            new byte[]
            {
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
            });
    }

    // Override method to patch FieldAccessException
    protected override PatchData Patch_FieldAccessException()
    {
        return new PatchData("mono_method_can_access_field", 0x120, new byte[] { 0x20, 0x00, 0x80, 0x52 });
    }

    // Override method to fix mono_class_from_mono_type_internal crash
    protected override PatchData Patch_mono_class_from_mono_type_internalCrashFix()
    {
        return new PatchData("mono_class_from_mono_type_internal", 0x23c,
            new byte[]
            {
                0x1f, 0x01, 0x00, 0xf1,
                0x20, 0x01, 0x88, 0x9a,
                0xfd, 0x7b, 0xc1, 0xa8,
                0xc0, 0x03, 0x5f, 0xd6,
            });
    }

    // Override method to get the target hash of the library
    protected override string GetLibHashTarget()
    {
        return "3a2ae3237b0be6d5ed7c4bda0b2c5fa8b2836a0a6de20fc96b007fb7389571b4";
    }
}