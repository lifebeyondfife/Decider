/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp
{
    public class ExpressionIntegerTest
    {
        private readonly Bounds<int> EnforceTrue = new Bounds<int>(1, 1);
        private readonly Bounds<int> EnforceFalse = new Bounds<int>(0, 0);

        [Fact]
        public void TestAddBoundsCorrect()
        {
            var variable1 = new VariableInteger("var1", 0, 5);
            var variable2 = new VariableInteger("var2", 0, 5);

            var expression = variable1 + variable2;
            var updatedBounds = expression.GetUpdatedBounds();

            Assert.Equal(0, updatedBounds.LowerBound);
            Assert.Equal(10, updatedBounds.UpperBound);
        }

        [Fact]
        public void TestSubtractBoundsCorrect()
        {
            var variable1 = new VariableInteger("var1", 0, 5);
            var variable2 = new VariableInteger("var2", 0, 5);

            var expression = variable1 - variable2;
            var updatedBounds = expression.GetUpdatedBounds();

            Assert.Equal(-5, updatedBounds.LowerBound);
            Assert.Equal(5, updatedBounds.UpperBound);
        }

        [Fact]
        public void TestMultiplyBoundsCorrect()
        {
            var variable1 = new VariableInteger("var1", 0, 5);
            var variable2 = new VariableInteger("var2", 0, 5);

            var expression = variable1 * variable2;
            var updatedBounds = expression.GetUpdatedBounds();

            Assert.Equal(0, updatedBounds.LowerBound);
            Assert.Equal(25, updatedBounds.UpperBound);
        }

        [Fact]
        public void TestDivideBoundsCorrect()
        {
            var divisor = new VariableInteger("divisor", 1, 5);

            var expression = 10 / divisor;
            var updatedBounds = expression.GetUpdatedBounds();

            Assert.Equal(2, updatedBounds.LowerBound);
            Assert.Equal(10, updatedBounds.UpperBound);
        }

        [Fact]
        public void TestXorBoundsCorrect()
        {
            var variable1 = new VariableInteger("var1", 0, 0);
            var variable2 = new VariableInteger("var2", 1, 1);

            var expression = variable1 ^ variable2;
            var updatedBounds = expression.GetUpdatedBounds();

            Assert.Equal(1, updatedBounds.LowerBound);
            Assert.Equal(1, updatedBounds.UpperBound);
        }
    }
}
