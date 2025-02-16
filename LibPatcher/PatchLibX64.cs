using ELFSharp.ELF.Sections;
using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace LibPatcher;

internal sealed class PatchLibX64 : BasePatchLib
{
    // Private constructor to initialize the base class with X64 platform
    private PatchLibX64() : base(PlatformEnum.X64)
    {
    }

    // Static method to start the patching process
    internal static void Start()
    {
        new PatchLibX64();
    }

    // Override method to patch FieldAccessException
    protected override PatchData Patch_FieldAccessException()
    {
        return new PatchData("mono_method_can_access_field", 0x132,
            new byte[]
            {
                0xB8, 0x01, 0x00, 0x00, 0x00, // MOV EAX, 1
                0x48, 0x83, 0xC4, 0x58,       // ADD RSP, 0x58
                0x5B,                         // POP RBX
                0x41, 0x5C,                   // POP R12
                0x41, 0x5D,                   // POP R13
                0x41, 0x5E,                   // POP R14
                0x41, 0x5F,                   // POP R15
                0x5D,                         // POP RBP
                0xC3                          // RET
            });
    }

    // Override method to patch MethodAccessException
    protected override PatchData Patch_MethodAccessException()
    {
        return new PatchData("mono_method_can_access_method", 0x30 + 0x15,
            new byte[]
            {
                0xEB, 0x09 // JMP to return
            });
    }

    // Override method to fix mono_class_from_mono_type_internal crash
    protected override PatchData Patch_mono_class_from_mono_type_internalCrashFix()
    {
        return new PatchData("mono_class_from_mono_type_internal", 0x261,
            new byte[]
            {
                0xB8, 0x00, 0x00, 0x00, 0x00, // MOV EAX, 0
                0x59,                         // POP RCX
                0xC3                          // RET
            });
    }

    // Override method to get the target hash of the library
    protected override string GetLibHashTarget()
    {
        return "57caffd67717aa21dabb276e629bdfb1c5451293e0cd1e5585f1a91dea359539";
    }
}
