/*
 * See LICENSE file
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLua;
using NUnit.Framework;

namespace LuaUnits
{
    [TestFixture]
    public static class LuaObjectTests
    {
        [Test]
        public static void ObjectEqualityNumber()
        {
            LuaObject obj1 = 10;
            LuaObject obj2 = 10;

            Assert.Multiple(() =>
            {
                Assert.That(obj1, Is.EqualTo(obj2));
                Assert.That(obj2, Is.EqualTo(obj1));
            });
        }

        [Test]
        public static void ObjectEqualityString()
        {
            LuaObject obj1 = "test";
            LuaObject obj2 = "test";

            Assert.Multiple(() =>
            {
                Assert.That(obj1, Is.EqualTo(obj2));
                Assert.That(obj2, Is.EqualTo(obj1));
            });
        }

        [Test]
        public static void ObjectEqualityCoercion()
        {
            LuaObject obj1 = "10";
            LuaObject obj2 = 10;

            Assert.Multiple(() =>
            {
                Assert.That(obj1, Is.Not.EqualTo(obj2));
                Assert.That(obj2, Is.Not.EqualTo(obj1));
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("nil")]
        [TestCase(0d)]
        public static void GeneralEquality(object? obj)
        {
            LuaObject a = LuaObject.FromObject(obj);

            Assert.That(a.Equals(obj), Is.True);
        }

        [Test]
        public static void LogicalOperators()
        {
            LuaObject a = "test";
            LuaObject b = LuaObject.Nil;

            Assert.IsTrue((a | b) == a);
            Assert.IsTrue((a | null) == a);

            Assert.IsTrue((a & b) == b);
            Assert.IsTrue((a & null) == LuaObject.Nil);
        }
    }
}
