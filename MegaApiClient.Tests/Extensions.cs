using System;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CG.Web.MegaApiClient.Tests
{
    public static class Extensions
    {
        // http://blog.drorhelper.com/2012/11/making-string-based-method-strongly.html?m=1
        public static ResolvableConstraintExpression Property<T>(this ConstraintExpression expression, Expression<Func<T, object>> lambda)
        {
            MemberExpression memberExpression = null;
            switch (lambda.Body.NodeType)
            {
                case ExpressionType.Convert:
                    // lambda is obj => Convert(obj.Prop) - the operand of conversion is our member expression
                    var unary = lambda.Body as UnaryExpression;
                    if (unary == null)
                    {
                        Assert.Fail("Cannot parse expression");
                    }
                    memberExpression = unary.Operand as MemberExpression;
                    break;

                case ExpressionType.MemberAccess:
                    // lambda is (obj => obj.Prop) - the body is the member expression
                    memberExpression = lambda.Body as MemberExpression;
                    break;

                default:
                    Assert.Fail("Cannot parse expression");
                    break;
            }

            if (memberExpression == null || string.IsNullOrEmpty(memberExpression.Member.Name))
            {
                Assert.Fail("Labda body is not MemberExpression - use only properties");
            }

            var propertyName = memberExpression.Member.Name;

            return expression.Property(propertyName);
        }
    }
}
