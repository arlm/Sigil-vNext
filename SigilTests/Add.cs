﻿using NUnit.Framework;
using Sigil;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SigilTests
{
    [TestFixture]
    public unsafe partial class Add
    {
        private delegate int* PointerToPointerDelegate(int by, int* ptr);

        [Test]
        public unsafe void PointerToPointer()
        {
            var e1 = Emit<PointerToPointerDelegate>.NewDynamicMethod();
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.Add();
            e1.Return();

            var d1 = e1.CreateDelegate();

            int* ptr1 = (int*)Marshal.AllocHGlobal(64);

            var ptr2 = d1(4, ptr1);

            Marshal.FreeHGlobal((IntPtr)ptr1);

            Assert.AreEqual(((int)ptr1) + 4, (int)ptr2);
        }

        private delegate void ByRefToByRefDelegate(ref int a, int b, ref int c);

        [Test]
        public unsafe void ByRefToByRef()
        {
            var e1 = Emit<ByRefToByRefDelegate>.NewDynamicMethod();
            e1.LoadArgument(1);
            e1.LoadArgument(0);
            e1.Add();
            e1.StoreArgument(2);
            e1.Return();

            var d1 = e1.CreateDelegate();

            int a = 2;
            d1(ref a, 4, ref a);

            Assert.AreEqual(2, a);
        }

        [Test]
        public void BlogPost()
        {
            {
                var method = new DynamicMethod("AddOneAndTwo", typeof(int), Type.EmptyTypes);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, 1);
                il.Emit(OpCodes.Ldc_I4, 2);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ret);

                var del = (Func<int>)method.CreateDelegate(typeof(Func<int>));

                Assert.AreEqual(3, del());
            }

            {
                var method = new DynamicMethod("AddOneAndTwo", typeof(int), Type.EmptyTypes);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, 1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ret);

                var del = (Func<int>)method.CreateDelegate(typeof(Func<int>));

                try
                {
                    del();
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Common Language Runtime detected an invalid program.", e.Message);
                }
            }

            {

                try
                {
                    var il = Emit<Func<int>>.NewDynamicMethod("AddOneAndTwo");
                    il.LoadConstant(1);
                    il.Add();
                    il.Return();

                    var del = il.CreateDelegate();

                    del();
                    Assert.Fail();
                }
                catch (SigilVerificationException e)
                {
                    Assert.AreEqual("Add expects 2 values on the stack", e.Message);
                }
            }
        }

        [Test]
        public void IntInt()
        {
            var il = Emit<Func<int>>.NewDynamicMethod("IntInt");
            il.LoadConstant(1);
            il.LoadConstant(2);
            il.Add();
            il.Return();

            var del = il.CreateDelegate();
            Assert.AreEqual(3, del());
        }

        [Test]
        public void IntNativeInt()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");
            e1.LoadConstant(1);
            e1.Convert<IntPtr>();
            e1.LoadConstant(3);
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(4, d1());
        }

        [Test]
        public void NativeIntInt()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");
            e1.LoadConstant(1);
            e1.LoadConstant(3);
            e1.Convert<IntPtr>();
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(4, d1());
        }

        [Test]
        public void NativeIntNativeInt()
        {
            var e1 = Emit<Func<int>>.NewDynamicMethod("E1");
            e1.LoadConstant(1);
            e1.Convert<IntPtr>();
            e1.LoadConstant(3);
            e1.Convert<IntPtr>();
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(4, d1());
        }

        [Test]
        public void IntPointer()
        {
            var e1 = Emit<Func<int, int>>.NewDynamicMethod("E1");
            e1.LoadArgumentAddress(0);
            e1.LoadConstant(2);
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            try
            {
                var x = d1(3);

                Assert.IsTrue(x != 0);
            }
            catch
            {
                Assert.Fail("ShouldBeLegal");
            }
        }

        [Test]
        public void PointerInt()
        {
            var e1 = Emit<Func<int, int>>.NewDynamicMethod("E1");
            e1.LoadConstant(2);
            e1.LoadArgumentAddress(0);
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            try
            {
                var x = d1(3);

                Assert.IsTrue(x != 0);
            }
            catch
            {
                Assert.Fail("ShouldBeLegal");
            }
        }

        [Test]
        public void PointerNativeInt()
        {
            var e1 = Emit<Func<int, int>>.NewDynamicMethod("E1");
            e1.LoadConstant(2);
            e1.Convert<IntPtr>();
            e1.LoadArgumentAddress(0);
            e1.Add();
            e1.Convert<int>();
            e1.Return();

            var d1 = e1.CreateDelegate();

            try
            {
                var x = d1(3);

                Assert.IsTrue(x != 0);
            }
            catch
            {
                Assert.Fail("ShouldBeLegal");
            }
        }

        [Test]
        public void LongLong()
        {
            var e1 = Emit<Func<long, long, long>>.NewDynamicMethod("E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.Add();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(2 * ((long)uint.MaxValue), d1(uint.MaxValue, uint.MaxValue));
        }

        [Test]
        public void FloatFloat()
        {
            var e1 = Emit<Func<float, float, float>>.NewDynamicMethod("E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.Add();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(3.14f + 1.59f, d1(3.14f, 1.59f));
        }

        [Test]
        public void DoubleDouble()
        {
            var e1 = Emit<Func<double, double, double>>.NewDynamicMethod("E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.Add();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(3.14 + 1.59, d1(3.14, 1.59));
        }

        [Test]
        public void Overflow()
        {
            var e1 = Emit<Func<double, double, double>>.NewDynamicMethod("E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.AddOverflow();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(3.14 + 1.59, d1(3.14, 1.59));
        }

        [Test]
        public void UnsignedOverflow()
        {
            var e1 = Emit<Func<double, double, double>>.NewDynamicMethod("E1");
            e1.LoadArgument(0);
            e1.LoadArgument(1);
            e1.UnsignedAddOverflow();
            e1.Return();

            var d1 = e1.CreateDelegate();

            Assert.AreEqual(3.14 + 1.59, d1(3.14, 1.59));
        }
    }
}
