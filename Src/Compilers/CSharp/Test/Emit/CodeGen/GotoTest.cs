﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class GotoStatementTest : EmitMetadataTestBase
    {
        [Fact]
        public void Goto()
        {
            string source = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""foo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
        return;
    }
}
";
            string expectedOutput = @"foo
bar
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        // Identical to last test, but without "return" statement.  (This was failing once.)
        [Fact]
        public void GotoWithoutReturn()
        {
            string source = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""foo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
    }
}
";
            string expectedOutput = @"foo
bar
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        // The goto can also be used to jump to a case or default statement in a switch
        [Fact]
        public void GotoInSwitch()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        string Fruit = ""Apple"";
        switch (Fruit)
        {
            case ""Banana"":
                break;
            case ""Chair"":
                break;
            case ""Apple"":
                goto case ""Banana"";
            case ""Table"":
                goto default;
            default:
                break;
        }
    }
}
";
            string expectedIL = @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (string V_0) //Fruit
  IL_0000:  ldstr      ""Apple""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""Banana""
  IL_000c:  call       ""bool string.op_Equality(string, string)""
  IL_0011:  brtrue.s   IL_0039
  IL_0013:  ldloc.0
  IL_0014:  ldstr      ""Chair""
  IL_0019:  call       ""bool string.op_Equality(string, string)""
  IL_001e:  brtrue.s   IL_0039
  IL_0020:  ldloc.0
  IL_0021:  ldstr      ""Apple""
  IL_0026:  call       ""bool string.op_Equality(string, string)""
  IL_002b:  brtrue.s   IL_0039
  IL_002d:  ldloc.0
  IL_002e:  ldstr      ""Table""
  IL_0033:  call       ""bool string.op_Equality(string, string)""
  IL_0038:  pop
  IL_0039:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Goto location outside enclosing block 
        [WorkItem(527952, "DevDiv")]
        [Fact]
        public void LocationOfGotoOutofClosure()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        if (i == 1)
            goto Lab1;
        else
            goto Lab2;
    Lab1:
        i = 2;
    Lab2:
        i = 3;

        System.Console.WriteLine(i);
    }
}
";
            string expectedIL =
@"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  pop
  IL_0003:  pop
  IL_0004:  ldc.i4.3
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Goto location in enclosing block  
        [WorkItem(527952, "DevDiv")]
        [Fact]
        public void LocationOfGotoInClosure()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        if (i == 1)
            goto Lab1;
        else
            goto Lab2;
    Lab1:
        i = 2;
    Lab2:
        i = 3;

        System.Console.WriteLine(i);
    }
}
";
            string expectedIL = @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  pop
  IL_0003:  pop
  IL_0004:  ldc.i4.3
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ret
}";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Same label in different scope  
        [WorkItem(539876, "DevDiv")]
        [Fact]
        public void SameLabelInDiffScope()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        {
        Lab1:
            goto Lab1;
        }
        {
        Lab1:
            return;
        }
    }
}
";
            string expectedIL = @"{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(
            // (11,9): warning CS0162: Unreachable code detected
            //         Lab1:
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "Lab1"),
            // (11,9): warning CS0164: This label has not been referenced
            //         Lab1:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Lab1")
                );
        }

        // Label Next to Label  
        [WorkItem(539877, "DevDiv")]
        [Fact]
        public void LabelNexttoLabel()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
    Lab3:
    // Lab1:
        goto Lab2;
    Lab2:
        goto Lab3;
    }
}
";
            string expectedIL = @"{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Infinite loop
        [WorkItem(527952, "DevDiv")]
        [Fact]
        public void Infiniteloop()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
    A:
        goto B;
    B:
        goto A;
    }
}
";
            string expectedIL = @"{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics();
        }

        // unreachable code
        [WorkItem(527952, "DevDiv")]
        [Fact]
        public void CS0162WRN_UnreachableCode()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5;)
        {
            i = 2;
            goto Lab2;
            i = 1;
            break;
        Lab2:
            return ;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0007
  IL_0004:  ldc.i4.2  
  IL_0005:  stloc.0   
  IL_0006:  ret       
  IL_0007:  ldloc.0   
  IL_0008:  ldc.i4.5  
  IL_0009:  blt.s      IL_0004
  IL_000b:  ret       
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "i"));
        }

        // Declare variable after goto
        [WorkItem(527952, "DevDiv")]
        [Fact]
        public void DeclareVariableAfterGoto()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        goto label1;
        string s = ""A""; // unreachable
    label1:
        s = ""B"";
        System.Console.WriteLine(s);
    }
}
";
            string expectedIL = @"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""B""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL).
                VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "string"));
        }

        // Finally is executed while use ‘goto’ to exit try block
        [WorkItem(540721, "DevDiv")]
        [Fact]
        public void GotoInTry()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        try { i = 1; goto lab1; }
        catch { i = 2; }
        finally { System.Console.WriteLine(""a""); }
    lab1:
        System.Console.WriteLine(i);
        return;
    }
}
";
            string expectedIL = @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_0016
}
  catch object
{
  IL_0006:  pop
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  leave.s    IL_0016
}
}
  finally
{
  IL_000b:  ldstr      ""a""
  IL_0010:  call       ""void System.Console.WriteLine(string)""
  IL_0015:  endfinally
}
  IL_0016:  ldloc.0
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  ret
}";
            CompileAndVerify(text, expectedOutput: @"a
1").
                VerifyIL("C.Main", expectedIL);
        }

        [WorkItem(540716, "DevDiv")]
        [Fact]
        public void GotoInFinallyBlock()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        try { i = 1; }
        catch { i = 2; }
        finally { lab1: i = 3; goto lab1; }

        System.Console.WriteLine(i);
    }
}
";
            string expectedIL = @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_000f
}
  catch object
{
  IL_0006:  pop
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  leave.s    IL_000f
}
}
  finally
{
  IL_000b:  ldc.i4.3
  IL_000c:  stloc.0
  IL_000d:  br.s       IL_000b
}
  IL_000f:  br.s       IL_000f
}";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Optimization redundant branch for code generate
        [Fact, WorkItem(527952, "DevDiv")]
        public void OptimizationForGoto()
        {
            var source = @"
class C
{
    static int Main(string[] args)
    {
        goto Lab1;
    Lab1:
        goto Lab2;
    Lab2:
        goto Lab3;
    Lab3:
        goto Lab4;
    Lab4:
        return 0;
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.OptimizedDll);

            c.VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
        }

        [Fact, WorkItem(528010, "DevDiv")]
        public void GotoInLambda()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del q = x =>
        {
        label2: goto label1;
        label1: goto label2;
        };

        System.Console.WriteLine(q);
    }
}
";
            string expectedIL = @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C.<Main>b__0(object, int)""
  IL_0010:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_001b:  call       ""void System.Console.WriteLine(object)""
  IL_0020:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Definition same label in different lambdas
        [WorkItem(5991, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SameLabelInDiffLambda()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del q = x =>
        {
            goto label1;
                label1:
                    return x * x;
        };
        System.Console.WriteLine(q);

        del p = x =>
        {
            goto label1;
                label1:
                    return x * x;
        };
        System.Console.WriteLine(p);

    }
}
";
            string expectedIL = @"
{
  // Code size       65 (0x41)
  .maxstack  2
  IL_0000:  ldsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C.<Main>b__0(object, int)""
  IL_0010:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_001b:  call       ""void System.Console.WriteLine(object)""
  IL_0020:  ldsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_003b
  IL_0028:  pop
  IL_0029:  ldnull
  IL_002a:  ldftn      ""int C.<Main>b__2(object, int)""
  IL_0030:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""del C.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_003b:  call       ""void System.Console.WriteLine(object)""
  IL_0040:  ret
}
";
            CompileAndVerify(text).
                VerifyIL("C.Main", expectedIL);
        }

        // Control is transferred to the target of the goto statement after finally
        [WorkItem(540720, "DevDiv")]
        [Fact]
        public void ControlTransferred()
        {
            var text = @"
class C
{
    public static void Main()
    {
        try { goto Label1; }
        catch { throw; }
        finally { Finally(); }
    Label1: Label();
    }
    private static void Finally()
    { System.Console.WriteLine(""Finally""); }
    private static void Label()
    { System.Console.WriteLine(""Label""); }
}
";
            string expectedOutput = @"Finally
Label
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Control is transferred to the target of the goto statement in nested try
        [WorkItem(540720, "DevDiv")]
        [Fact]
        public void ControlTransferred_02()
        {
            var text = @"
class C
{
    public static void Main()
    {
        int i = 0;
        try
        {
            i = 1;
            try { goto lab1; }
            catch { throw; }
            finally { System.Console.WriteLine(""inner finally""); }
        }
        catch { i = 2; }
        finally { System.Console.WriteLine(""outer finally""); }
    lab1: System.Console.WriteLine(""label"");
    }
}
";
            string expectedOutput = @"inner finally
outer finally
label
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ControlTransferred_03()
        {
            var text = @"
using System.Collections;
class C
{
    public static void Main()
    {
        foreach (int i in Power(2, 3))
        {
            System.Console.WriteLine(i);
        }
    }
    public static IEnumerable Power(int number, int exponent)
    {
        int counter = 0;
        int result = 1;
        try
        {
            while (counter++ < exponent)
            {
                result = result * number;
                yield return result;
            }
            goto Label1;
        }
        finally { System.Console.WriteLine(""finally""); }
    Label1: System.Console.WriteLine(""label"");
    }
}
";
            string expectedOutput = @"2
4
8
finally
label
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // When ReflectionEmit supports writing exception handler info, this method
        // can be removed and CompileAndVerify references above will resolve to
        // the overload that emits with both CCI and ReflectionEmit. (Bug #7012)
        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null)
        {
            return base.CompileAndVerify(source: source, emitOptions: EmitOptions.CCI, expectedOutput: expectedOutput);
        }

        [WorkItem(540719, "DevDiv")]
        [Fact]
        public void LabelBetweenLocalAndInitialize()
        {
            var text = @"
class C
{
    static void M(int x)
    {
    NoInitializers:
        int w, y;
    Const1:
    Const2:
        const int z = 0;
        w = z;
        y = x + w;
        x = y;
    }
}";
            CompileAndVerify(text);
        }

        [WorkItem(540719, "DevDiv")]
        [Fact]
        public void LabelBetweenLocalAndInitialize02()
        {
            var text = @"
public class A
{
    public static int Main()
    {
        int i = 0;
        int retVal = 1;
        try
        {
        L:
            int k;
            try
            {
                i++; goto L;
            }
            finally
            {
                if (i == 10) throw new System.Exception();
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""Catch"");
            retVal = 0;
        }
        return retVal;
    }
}
";
            CompileAndVerify(text, expectedOutput: "Catch");
        }

        [WorkItem(540719, "DevDiv")]
        [Fact]
        public void LabelBetweenLocalAndInitialize03()
        {
            var text = @"
public class A
{
    public static int Main()
    {
        int i = 0;
        int retVal = 1;
        try
        {
        L:
            { int k; }
            try
            {
                i++; goto L;
            }
            finally
            {
                if (i == 10) throw new System.Exception();
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""Catch"");
            retVal = 0;
        }
        return retVal;
    }
}
";
            CompileAndVerify(text, expectedOutput: "Catch");
        }

    }
}
