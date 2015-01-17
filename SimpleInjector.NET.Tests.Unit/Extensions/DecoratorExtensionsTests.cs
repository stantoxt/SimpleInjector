﻿namespace SimpleInjector.Tests.Unit.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SimpleInjector.Advanced;
    using SimpleInjector.Extensions;

    [TestClass]
    public class DecoratorExtensionsTests
    {
        [TestMethod]
        public void GetInstance_OnRegisteredPartialGenericDecoratorType_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<List<int>>, NullCommandHandler<List<int>>>();

            // ClassConstraintHandlerDecorator<List<T>>
            var partialOpenGenericDecoratorType =
                typeof(ClassConstraintHandlerDecorator<>).MakeGenericType(typeof(List<>));

            container.RegisterDecorator(typeof(ICommandHandler<>), partialOpenGenericDecoratorType);

            // Act
            var instance = container.GetInstance<ICommandHandler<List<int>>>();

            // Assert
            Assert.AreEqual(
                typeof(ClassConstraintHandlerDecorator<List<int>>).ToFriendlyName(),
                instance.GetType().ToFriendlyName(),
                "Decorator was not applied.");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedNonGenericType_ReturnsTheDecoratedService()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator));

            var decorator = (NonGenericServiceDecorator)service;

            Assert.IsInstanceOfType(decorator.DecoratedService, typeof(RealNonGenericService));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedNonGenericSingleton_ReturnsTheDecoratedService()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator));

            var decorator = (NonGenericServiceDecorator)service;

            Assert.IsInstanceOfType(decorator.DecoratedService, typeof(RealNonGenericService));
        }

        [TestMethod]
        public void GetInstance_SingleInstanceWrappedByATransientDecorator_ReturnsANewDecoratorEveryTime()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var decorator1 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();
            var decorator2 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsFalse(object.ReferenceEquals(decorator1, decorator2),
                "A new decorator should be created on each call to GetInstance().");
            Assert.IsTrue(object.ReferenceEquals(decorator1.DecoratedService, decorator2.DecoratedService),
                "The same instance should be wrapped on each call to GetInstance().");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedNonGenericType_DecoratesInstanceWithExpectedLifeTime()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Register as transient
            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var decorator1 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();
            var decorator2 = (NonGenericServiceDecorator)container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsFalse(object.ReferenceEquals(decorator1.DecoratedService, decorator2.DecoratedService),
                "The decorated instance is expected to be a transient.");
        }

        [TestMethod]
        public void RegisterDecorator_RegisteringAnOpenGenericDecoratorWithANonGenericService_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => 
                container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(@"
                The supplied decorator NonGenericServiceDecorator<T> is an open
                generic type definition".TrimInside(),
                action);

            AssertThat.ThrowsWithParamName("decoratorType", action);
        }

        [TestMethod]
        public void GetInstance_OnNonGenericTypeDecoratedWithGenericDecorator_ReturnsTheDecoratedService()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<INonGenericService, RealNonGenericService>();
            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator<int>));

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator<int>));

            var decorator = (NonGenericServiceDecorator<int>)service;

            Assert.IsInstanceOfType(decorator.DecoratedService, typeof(RealNonGenericService));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_ReturnsTheDecorator()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(RealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatMatchesTheRequestedService1_ReturnsTheDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<RealCommand>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void RegisterDecorator_WithClosedGenericServiceAndOpenGenericDecorator_FailsWithExpectedException()
        {
            // Arrange
            string expectedMessage = @"
                Registering a closed generic service type with an open generic decorator is not supported. 
                Instead, register the service type as open generic, and the decorator as closed generic 
                type."
                .TrimInside();

            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            // Act
            Action action = () => container.RegisterDecorator(
                typeof(ICommandHandler<RealCommand>),
                typeof(TransactionHandlerDecorator<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<NotSupportedException>(expectedMessage, action);
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatMatchesTheRequestedService2_ReturnsTheDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(
                typeof(ICommandHandler<RealCommand>),
                typeof(TransactionHandlerDecorator<RealCommand>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatDoesNotMatchTheRequestedService1_ReturnsTheServiceItself()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<int>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandler));
        }

        [TestMethod]
        public void GetInstance_WithExplicitGenericImplementionRegisteredAsDecoratorThatDoesNotMatchTheRequestedService2_ReturnsTheServiceItself()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<int>), typeof(TransactionHandlerDecorator<int>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandler));
        }

        [TestMethod]
        public void GetInstance_NonGenericDecoratorForMatchingClosedGenericServiceType_ReturnsTheNonGenericDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new RealCommandHandler());

            Type closedGenericServiceType = typeof(ICommandHandler<RealCommand>);
            Type nonGenericDecorator = typeof(RealCommandHandlerDecorator);

            container.RegisterDecorator(closedGenericServiceType, nonGenericDecorator);

            // Act
            var decorator = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(decorator, nonGenericDecorator);
        }

        [TestMethod]
        public void GetInstance_NonGenericDecoratorForNonMatchingClosedGenericServiceType_ThrowsAnException()
        {
            // Arrange
            var container = ContainerFactory.New();

            Type nonMathcingClosedGenericServiceType = typeof(ICommandHandler<int>);

            // Decorator implements ICommandHandler<RealCommand>
            Type nonGenericDecorator = typeof(RealCommandHandlerDecorator);

            // Act
            Action action = 
                () => container.RegisterDecorator(nonMathcingClosedGenericServiceType, nonGenericDecorator);

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "The supplied type RealCommandHandlerDecorator does not implement ICommandHandler<Int32>", 
                action);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_GetsHandledAsExpected()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = ContainerFactory.New();

            container.RegisterSingle<ILogger>(logger);

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(LoggingRealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>().Handle(new RealCommand());

            // Assert
            Assert.AreEqual("Begin1 RealCommand End1", logger.Message);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_ReturnsLastRegisteredDecorator()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(RealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LogExceptionCommandHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(LogExceptionCommandHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_GetsHandledAsExpected()
        {
            // Arrange
            var logger = new FakeLogger();

            var container = ContainerFactory.New();

            container.RegisterSingle<ILogger>(logger);

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(LoggingRealCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator2<>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>().Handle(new RealCommand());

            // Assert
            Assert.AreEqual("Begin2 Begin1 RealCommand End1 End2", logger.Message);
        }

        [TestMethod]
        public void GetInstance_WithInitializerOnDecorator_InitializesThatDecorator()
        {
            // Arrange
            int expectedItem1Value = 1;
            string expectedItem2Value = "some value";

            var container = ContainerFactory.New();

            container.RegisterInitializer<HandlerDecoratorWithPropertiesBase>(decorator =>
            {
                decorator.Item1 = expectedItem1Value;
            });

            container.RegisterInitializer<HandlerDecoratorWithPropertiesBase>(decorator =>
            {
                decorator.Item2 = expectedItem2Value;
            });

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(HandlerDecoratorWithProperties<>));

            // Act
            var handler =
                (HandlerDecoratorWithPropertiesBase)container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedItem1Value, handler.Item1, "Initializer did not run.");
            Assert.AreEqual(expectedItem2Value, handler.Item2, "Initializer did not run.");
        }

        [TestMethod]
        public void GetInstance_DecoratorWithMissingDependency_ThrowAnExceptionWithADescriptiveMessage()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            // Decorator1Handler depends on ILogger, but ILogger is not registered.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>("ILogger", action);
        }

        [TestMethod]
        public void GetInstance_DecoratorPredicateReturnsFalse_DoesNotDecorateInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c => false);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StubCommandHandler));
        }

        [TestMethod]
        public void GetInstance_DecoratorPredicateReturnsTrue_DecoratesInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>),
                c => true);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_CallsThePredicateWithTheExpectedServiceType()
        {
            // Arrange
            Type expectedPredicateServiceType = typeof(ICommandHandler<RealCommand>);
            Type actualPredicateServiceType = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateServiceType = c.ServiceType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateServiceType, actualPredicateServiceType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTransient_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTransientWithInitializer_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterInitializer<StubCommandHandler>(handlerToInitialize => { });

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_SingletonDecoratorWithInitializer_ShouldReturnSingleton()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            container.RegisterSingleDecorator(typeof(ICommandHandler<>), typeof(AsyncCommandHandlerProxy<>));

            container.RegisterInitializer<AsyncCommandHandlerProxy<RealCommand>>(handler => { });

            // Act
            var handler1 = container.GetInstance<ICommandHandler<RealCommand>>();
            var handler2 = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(AsyncCommandHandlerProxy<RealCommand>));

            Assert.IsTrue(object.ReferenceEquals(handler1, handler2),
                "GetInstance should always return the same instance, since AsyncCommandHandlerProxy is " +
                "registered as singleton.");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedSingleton_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = ContainerFactory.New();

            container.RegisterSingle<ICommandHandler<RealCommand>>(new StubCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedTypeRegisteredWithFuncDelegate_CallsThePredicateWithTheImplementationTypeEqualsServiceType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(ICommandHandler<RealCommand>);
            Type actualPredicateImplementationType = null;

            var container = ContainerFactory.New();

            // Because we register a Func<TServiceType> there is no way we can determine the implementation 
            // type. In that case the ImplementationType should equal the ServiceType.
            container.Register<ICommandHandler<RealCommand>>(() => new StubCommandHandler());

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateImplementationType = c.ImplementationType;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_CallsThePredicateWithTheExpectedImplementationType()
        {
            // Arrange
            Type expectedPredicateImplementationType = typeof(StubCommandHandler);
            Type actualPredicateImplementationType = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), context =>
                {
                    actualPredicateImplementationType = context.ImplementationType;
                    return true;
                });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.AreEqual(expectedPredicateImplementationType, actualPredicateImplementationType);
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_CallsThePredicateWithAnExpression()
        {
            // Arrange
            Expression actualPredicateExpression = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                actualPredicateExpression = c.Expression;
                return true;
            });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsNotNull(actualPredicateExpression);
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances_SuppliesADifferentExpressionToTheSecondPredicate()
        {
            // Arrange
            Expression predicateExpressionOnFirstCall = null;
            Expression predicateExpressionOnSecondCall = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>), c =>
            {
                predicateExpressionOnFirstCall = c.Expression;
                return true;
            });

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), context =>
                {
                    predicateExpressionOnSecondCall = context.Expression;
                    return true;
                });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreNotEqual(predicateExpressionOnFirstCall, predicateExpressionOnSecondCall,
                "The predicate was expected to change, because the first decorator has been applied.");
        }

        [TestMethod]
        public void GetInstance_OnDecoratedType_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
                {
                    appliedDecorators = c.AppliedDecorators;
                    return true;
                });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(0, appliedDecorators.Count());
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances1_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
                {
                    appliedDecorators = c.AppliedDecorators;
                    return true;
                });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(1, appliedDecorators.Count());
            Assert.AreEqual(typeof(TransactionHandlerDecorator<RealCommand>), appliedDecorators.First());
        }

        [TestMethod]
        public void GetInstance_OnTypeDecoratedByMultipleInstances2_SuppliesNoAppliedDecoratorsToThePredicate()
        {
            // Arrange
            IEnumerable<Type> appliedDecorators = null;

            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>));

            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(LogExceptionCommandHandlerDecorator<>), c =>
                {
                    appliedDecorators = c.AppliedDecorators;
                    return true;
                });

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(2, appliedDecorators.Count());
            Assert.AreEqual(typeof(TransactionHandlerDecorator<RealCommand>), appliedDecorators.First());
            Assert.AreEqual(typeof(LogExceptionCommandHandlerDecorator<RealCommand>), appliedDecorators.Second());
        }

        [TestMethod]
        public void GetInstance_DecoratorThatSatisfiesRequestedTypesTypeConstraints_DecoratesThatInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(ClassConstraintHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(ClassConstraintHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_DecoratorThatDoesNotSatisfyRequestedTypesTypeConstraints_DoesNotDecorateThatInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StructCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(ClassConstraintHandlerDecorator<>));

            // Act
            var handler = container.GetInstance<ICommandHandler<StructCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StructCommandHandler));
        }

        [TestMethod]
        public void RegisterDecorator_DecoratorWithMultiplePublicConstructors_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(MultipleConstructorsCommandHandlerDecorator<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "it should contain exactly one public constructor",
                action);
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingTypeThatIsNotADecorator_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(InvalidDecoratorCommandHandlerDecorator<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(@"
                For the container to be able to use InvalidDecoratorCommandHandlerDecorator<T> as  
                a decorator, its constructor must include a single parameter of type 
                ICommandHandler<T>".TrimInside(),
                action);
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingTypeThatIsNotADecorator_ThrowsException2()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(InvalidDecoratorCommandHandlerDecorator<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "does not currently exist in the constructor",
                action);
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingTypeThatIsNotADecorator_ThrowsException3()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(InvalidDecoratorCommandHandlerDecorator2<>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "is defined multiple times in the constructor",
                action);
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingAnUnrelatedType_FailsWithExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = 
                () => container.RegisterDecorator(typeof(ICommandHandler<>), typeof(KeyValuePair<,>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(@"
                    The supplied type KeyValuePair<TKey, TValue> does not implement 
                    ICommandHandler<TCommand>.
                    ".TrimInside(),
                action);
        }

        [TestMethod]
        public void RegisterDecorator_SupplyingAConcreteNonGenericType_ShouldSucceed()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericType_ReturnsExpectedDecorator1()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StubCommandHandler));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericTypeThatDoesNotMatch_DoesNotReturnThatDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            // StructCommandHandler implements ICommandHandler<StructCommand>
            container.RegisterManyForOpenGeneric(typeof(ICommandHandler<>), typeof(StructCommandHandler));

            // ConcreteCommandHandlerDecorator implements ICommandHandler<RealCommand>
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<StructCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(StructCommandHandler));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorSupplyingAConcreteNonGenericType_ReturnsExpectedDecorator2()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();
            container.Register<ICommandHandler<StructCommand>, StructCommandHandler>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void RegisterDecorator_NonGenericDecoratorWithFuncAsConstructorArgument_InjectsAFactoryThatCreatesNewInstancesOfTheDecoratedType()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecoratorWithFunc));

            var decorator = (NonGenericServiceDecoratorWithFunc)container.GetInstance<INonGenericService>();

            Func<INonGenericService> factory = decorator.DecoratedServiceCreator;

            // Act
            // Execute the factory twice.
            INonGenericService instance1 = factory();
            INonGenericService instance2 = factory();

            // Assert
            Assert.IsInstanceOfType(instance1, typeof(RealNonGenericService),
                "The injected factory is expected to create instances of type RealNonGenericService.");

            Assert.IsFalse(object.ReferenceEquals(instance1, instance2),
                "The factory is expected to create transient instances, since that is how " +
                "RealNonGenericService is registered.");
        }

        [TestMethod]
        public void RegisterDecorator_GenericDecoratorWithFuncAsConstructorArgument_InjectsAFactoryThatCreatesNewInstancesOfTheDecoratedType()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<ILogger>(new FakeLogger());

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LogExceptionCommandHandlerDecorator<>));

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(AsyncCommandHandlerProxy<>));

            // Act
            var handler =
                (AsyncCommandHandlerProxy<RealCommand>)container.GetInstance<ICommandHandler<RealCommand>>();

            Func<ICommandHandler<RealCommand>> factory = handler.DecorateeFactory;

            // Execute the factory twice.
            ICommandHandler<RealCommand> instance1 = factory();
            ICommandHandler<RealCommand> instance2 = factory();

            // Assert
            Assert.IsInstanceOfType(instance1, typeof(LogExceptionCommandHandlerDecorator<RealCommand>),
                "The injected factory is expected to create instances of type " +
                "LogAndContinueCommandHandlerDecorator<RealCommand>.");

            Assert.IsFalse(object.ReferenceEquals(instance1, instance2),
                "The factory is expected to create transient instances.");
        }

        [TestMethod]
        public void RegisterDecorator_CalledWithDecoratorTypeWithBothAFuncAndADecorateeParameter_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.RegisterDecorator(typeof(INonGenericService),
                typeof(NonGenericServiceDecoratorWithBothDecorateeAndFunc));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "single parameter of type INonGenericService (or Func<INonGenericService>)",
                action);
        }

        [TestMethod]
        public void RegisterDecorator_RegisteringAClassThatWrapsADifferentClosedTypeThanItImplements_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            // BadCommandHandlerDecorator1<T> implements ICommandHandler<int> but wraps ICommandHandler<byte>
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                  typeof(BadCommandHandlerDecorator1));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(@"
                must include a single parameter of type ICommandHandler<Int32> (or Func<ICommandHandler<Int32>>)"
                .TrimInside(),
                action);
        }

        [TestMethod]
        public void RegisterDecorator_RegisteringADecoratorWithAnUnresolvableTypeArgument_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            // CommandHandlerDecoratorWithUnresolvableArgument<T, TUnresolved> contains a not-mappable 
            // type argument TUnresolved.
            Action action = () => container.RegisterDecorator(typeof(ICommandHandler<>),
                typeof(CommandHandlerDecoratorWithUnresolvableArgument<,>));

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "contains unresolvable type arguments.",
                action);

            AssertThat.ThrowsWithParamName("decoratorType", action);
        }

        [TestMethod]
        public void GetInstance_TypeRegisteredWithRegisterSingleDecorator_AlwaysReturnsTheSameInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterSingleDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator));

            // Act
            var decorator1 = container.GetInstance<INonGenericService>();
            var decorator2 = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(decorator1, typeof(NonGenericServiceDecorator));

            Assert.IsTrue(object.ReferenceEquals(decorator1, decorator2),
                "Since the decorator is registered as singleton, GetInstance should always return the same " +
                "instance.");
        }

        [TestMethod]
        public void GetInstance_TypeRegisteredWithRegisterSingleDecoratorPredicate_AlwaysReturnsTheSameInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<INonGenericService, RealNonGenericService>();

            container.RegisterSingleDecorator(typeof(INonGenericService), typeof(NonGenericServiceDecorator),
                c => true);

            // Act
            var decorator1 = container.GetInstance<INonGenericService>();
            var decorator2 = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(decorator1, typeof(NonGenericServiceDecorator));

            Assert.IsTrue(object.ReferenceEquals(decorator1, decorator2),
                "Since the decorator is registered as singleton, GetInstance should always return the same " +
                "instance.");
        }

        [TestMethod]
        public void Verify_DecoratorRegisteredThatCanNotBeResolved_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            // LoggingHandlerDecorator1 depends on ILogger, which is not registered.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            // Act
            Action action = () => container.Verify();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<InvalidOperationException>(@"
                The constructor of type LoggingHandlerDecorator1<RealCommand> 
                contains the parameter of type ILogger with name 'logger' that is 
                not registered.".TrimInside(),
                action);
        }

        [TestMethod]
        public void GetInstance_DecoratorRegisteredTwiceAsSingleton_WrapsTheDecorateeTwice()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Uses the RegisterAll<T>(IEnumerable<T>) that registers a dynamic list.
            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            // Register the same decorator twice. 
            container.RegisterSingleDecorator(
                typeof(ICommandHandler<>),
                typeof(TransactionHandlerDecorator<>));

            container.RegisterSingleDecorator(
                typeof(ICommandHandler<>),
                typeof(TransactionHandlerDecorator<>));

            // Act
            var decorator1 = (TransactionHandlerDecorator<RealCommand>)
                container.GetInstance<ICommandHandler<RealCommand>>();

            var decorator2 = decorator1.Decorated;

            // Assert
            Assert.IsInstanceOfType(decorator2, typeof(TransactionHandlerDecorator<RealCommand>),
                "Since the decorator is registered twice, it should wrap the decoratee twice.");

            var decoratee = ((TransactionHandlerDecorator<RealCommand>)decorator2).Decorated;

            Assert.IsInstanceOfType(decoratee, typeof(StubCommandHandler));
        }

        [TestMethod]
        public void HybridLifestyleRegistration_WithDecorator_DecoratesTheInstance()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => true, Lifestyle.Transient, Lifestyle.Singleton);

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(hybrid);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandlerDecorator));
        }

        [TestMethod]
        public void HybridLifestyleRegistration_WithTransientDecorator_AppliesTransientDecorator()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => false, Lifestyle.Singleton, Lifestyle.Singleton);

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(hybrid);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler1 = container.GetInstance<ICommandHandler<RealCommand>>();
            var handler2 = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsFalse(object.ReferenceEquals(handler1, handler2), "Decorator should be transient.");
        }

        [TestMethod]
        public void HybridLifestyleRegistration_WithTransientDecorator_DoesNotApplyDecoratorMultipleTimes()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => false, Lifestyle.Singleton, Lifestyle.Singleton);

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(hybrid);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler = (RealCommandHandlerDecorator)container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler.Decorated, typeof(StubCommandHandler));
        }

        [TestMethod]
        public void HybridLifestyleRegistration_WithTransientDecorator_LeavesTheLifestyleInTact1()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => false, Lifestyle.Singleton, Lifestyle.Singleton);

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(hybrid);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler1 = (RealCommandHandlerDecorator)container.GetInstance<ICommandHandler<RealCommand>>();
            var handler2 = (RealCommandHandlerDecorator)container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsTrue(object.ReferenceEquals(handler1.Decorated, handler2.Decorated),
                "The wrapped instance should have the expected lifestyle (singleton in this case).");
        }

        [TestMethod]
        public void HybridLifestyleRegistration_WithTransientDecorator_LeavesTheLifestyleInTact2()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => false, Lifestyle.Transient, Lifestyle.Transient);

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(hybrid);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // Act
            var handler1 = (RealCommandHandlerDecorator)container.GetInstance<ICommandHandler<RealCommand>>();
            var handler2 = (RealCommandHandlerDecorator)container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsFalse(object.ReferenceEquals(handler1.Decorated, handler2.Decorated),
                "The wrapped instance should have the expected lifestyle (transient in this case).");
        }

        [TestMethod]
        public void GetRegistration_TransientInstanceDecoratedWithTransientDecorator_ContainsTheExpectedRelationship()
        {
            // Arrange
            var expectedRelationship = new RelationshipInfo
            {
                Lifestyle = Lifestyle.Transient,
                ImplementationType = typeof(RealCommandHandlerDecorator),
                Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), Lifestyle.Transient)
            };

            var container = ContainerFactory.New();

            // StubCommandHandler has no dependencies.
            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            // RealCommandHandlerDecorator only has ICommandHandler<RealCommand> as dependency.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            container.Verify();

            // Act
            var actualRelationship = container.GetRegistration(typeof(ICommandHandler<RealCommand>))
                .GetRelationships()
                .Single();

            // Assert
            Assert.IsTrue(expectedRelationship.Equals(actualRelationship));
        }

        [TestMethod]
        public void GetRegistration_TransientInstanceDecoratedWithSingletonDecorator_ContainsTheExpectedRelationship()
        {
            // Arrange
            var hybrid = Lifestyle.CreateHybrid(() => true, Lifestyle.Transient, Lifestyle.Singleton);

            var expectedRelationships = new[]
            {   
                new RelationshipInfo
                {
                    Lifestyle = hybrid,
                    ImplementationType = typeof(RealCommandHandlerDecorator),
                    Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), Lifestyle.Transient)
                },
                new RelationshipInfo
                {
                    Lifestyle = Lifestyle.Singleton,
                    ImplementationType = typeof(RealCommandHandlerDecorator),
                    Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), hybrid)
                },
            };

            var container = ContainerFactory.New();

            // StubCommandHandler has no dependencies.
            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(
                Lifestyle.Transient);

            // RealCommandHandlerDecorator only has ICommandHandler<RealCommand> as dependency.
            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(RealCommandHandlerDecorator),
                hybrid);

            // RealCommandHandlerDecorator only has ICommandHandler<RealCommand> as dependency.
            container.RegisterDecorator(
                typeof(ICommandHandler<>),
                typeof(RealCommandHandlerDecorator),
                Lifestyle.Singleton);

            container.Verify();

            // Act
            var actualRelationships = container.GetRegistration(typeof(ICommandHandler<RealCommand>))
                .GetRelationships()
                .ToArray();

            // Assert
            Assert.IsTrue(
                actualRelationships.All(a => expectedRelationships.Any(e => e.Equals(a))),
                "actual: " + Environment.NewLine +
                string.Join(Environment.NewLine, actualRelationships.Select(r => RelationshipInfo.ToString(r))));
        }

        [TestMethod]
        public void GetRegistration_DecoratorWithNormalDependency_ContainsTheExpectedRelationship()
        {
            // Arrange
            var expectedRelationship1 = new RelationshipInfo
            {
                ImplementationType = typeof(LoggingHandlerDecorator1<RealCommand>),
                Lifestyle = Lifestyle.Transient,
                Dependency = new DependencyInfo(typeof(ILogger), Lifestyle.Singleton)
            };

            var expectedRelationship2 = new RelationshipInfo
            {
                ImplementationType = typeof(LoggingHandlerDecorator1<RealCommand>),
                Lifestyle = Lifestyle.Transient,
                Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), Lifestyle.Transient)
            };

            var container = ContainerFactory.New();

            container.RegisterSingle<ILogger, FakeLogger>();

            // StubCommandHandler has no dependencies.
            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>();

            // LoggingHandlerDecorator1 takes a dependency on ILogger.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(LoggingHandlerDecorator1<>));

            container.Verify();

            // Act
            var relationships =
                container.GetRegistration(typeof(ICommandHandler<RealCommand>)).GetRelationships();

            // Assert
            // I'm too lazy to split this up in two tests :-)
            Assert.AreEqual(1, relationships.Count(actual => expectedRelationship1.Equals(actual)));
            Assert.AreEqual(1, relationships.Count(actual => expectedRelationship2.Equals(actual)));
        }

        [TestMethod]
        public void GetRegistration_SingletonInstanceWithTransientDecoratorWithSingletonDecorator_ContainsExpectedRelationships()
        {
            // Arrange
            var expectedRelationship1 = new RelationshipInfo
            {
                ImplementationType = typeof(TransactionHandlerDecorator<RealCommand>),
                Lifestyle = Lifestyle.Singleton,
                Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), Lifestyle.Transient)
            };

            var expectedRelationship2 = new RelationshipInfo
            {
                ImplementationType = typeof(RealCommandHandlerDecorator),
                Lifestyle = Lifestyle.Transient,
                Dependency = new DependencyInfo(typeof(ICommandHandler<RealCommand>), Lifestyle.Singleton)
            };

            var container = ContainerFactory.New();

            container.RegisterSingle<ILogger, FakeLogger>();

            // StubCommandHandler has no dependencies.
            container.RegisterSingle<ICommandHandler<RealCommand>, StubCommandHandler>();

            // RealCommandHandlerDecorator only takes a dependency on ICommandHandler<RealCommand>
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            // TransactionHandlerDecorator<T> only takes a dependency on ICommandHandler<T>
            container.RegisterSingleDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.Verify();

            // Act
            var relationships =
                container.GetRegistration(typeof(ICommandHandler<RealCommand>)).GetRelationships();

            // Assert
            Assert.AreEqual(1, relationships.Count(actual => expectedRelationship1.Equals(actual)));
        }

        // This test was written for work item https://simpleinjector.codeplex.com/workitem/20141.
        [TestMethod]
        public void GetRelationships_DecoratorDependingOnFuncDecorateeFactory_ReturnsRelationshipForThatFactory()
        {
            // Arrange
            var expectedRelationship = new RelationshipInfo
            {
                Lifestyle = Lifestyle.Singleton,
                ImplementationType = typeof(NonGenericServiceDecoratorWithFunc),
                Dependency = new DependencyInfo(typeof(Func<INonGenericService>), Lifestyle.Singleton)
            };

            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>(Lifestyle.Transient);

            container.RegisterDecorator(typeof(INonGenericService),
                typeof(NonGenericServiceDecoratorWithFunc), Lifestyle.Singleton);

            container.Verify();

            // Act
            var relationships = container.GetRegistration(typeof(INonGenericService)).GetRelationships();

            // Assert
            var actualRelationship = relationships.Single();

            Assert.IsTrue(expectedRelationship.Equals(actualRelationship),
                "actual: " + RelationshipInfo.ToString(actualRelationship));
        }

        [TestMethod]
        public void GetRelationships_DecoratorDependingOnTransientFuncDecorateeFactory_ReturnsRelationshipForThatFactory()
        {
            // Arrange
            var expectedRelationship = new RelationshipInfo
            {
                Lifestyle = Lifestyle.Transient,
                ImplementationType = typeof(NonGenericServiceDecoratorWithFunc),
                Dependency = new DependencyInfo(typeof(Func<INonGenericService>), Lifestyle.Singleton)
            };

            var container = new Container();

            container.Register<INonGenericService, RealNonGenericService>(Lifestyle.Transient);

            // Here we register the decorator as transient!
            container.RegisterDecorator(typeof(INonGenericService),
                typeof(NonGenericServiceDecoratorWithFunc), Lifestyle.Transient);

            container.Verify();

            // Act
            var relationships = container.GetRegistration(typeof(INonGenericService)).GetRelationships();

            // Assert
            var actualRelationship = relationships.Single();

            Assert.IsTrue(expectedRelationship.Equals(actualRelationship),
                "actual: " + RelationshipInfo.ToString(actualRelationship));
        }

        [TestMethod]
        public void Lifestyle_TransientRegistrationDecoratedWithSingletonDecorator_GetsLifestyleOfDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(Lifestyle.Transient);

            container.RegisterSingleDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            var registration = container.GetRegistration(typeof(ICommandHandler<RealCommand>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(Lifestyle.Singleton, registration.Lifestyle);
        }

        [TestMethod]
        public void Lifestyle_SingletonRegistrationDecoratedWithTransientDecorator_GetsLifestyleOfDecorator()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(Lifestyle.Singleton);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            var registration = container.GetRegistration(typeof(ICommandHandler<RealCommand>));

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.AreEqual(Lifestyle.Transient, registration.Lifestyle);
        }

        [TestMethod]
        public void GetInstance_DecoratedInstance_DecoratorGoesThroughCompletePipeLineIncludingExpressionBuilding()
        {
            // Arrange
            var typesBuilding = new List<Type>();

            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>, StubCommandHandler>(Lifestyle.Singleton);

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));

            container.ExpressionBuilding += (s, e) =>
            {
                typesBuilding.Add(((NewExpression)e.Expression).Constructor.DeclaringType);
            };

            // Act
            container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsTrue(typesBuilding.Any(type => type == typeof(TransactionHandlerDecorator<RealCommand>)),
                "The decorator is expected to go through the complete pipeline, including ExpressionBuilding.");
        }

        [TestMethod]
        public void RegisterDecorator_DecoratorWithGenericTypeConstraintOtherThanTheClassConstraint_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            // Somehow the "where T : class" always works, while things like "where T : struct" or 
            // "where T : ISpecialCommand" (used here) doesn't.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(SpecialCommandHandlerDecorator<>));
        }

        [TestMethod]
        public void RegisterDecorator_DecoratorWithGenericTypeConstraint_WrapsTypesThatAdhereToTheConstraint()
        {
            // Arrange
            var container = ContainerFactory.New();

            // SpecialCommand implements ISpecialCommand
            container.Register<ICommandHandler<SpecialCommand>, NullCommandHandler<SpecialCommand>>();

            // SpecialCommandHandlerDecorator has a "where T : ISpecialCommand" constraint.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(SpecialCommandHandlerDecorator<>));

            // Act
            var specialHandler = container.GetInstance<ICommandHandler<SpecialCommand>>();

            // Assert
            Assert.IsInstanceOfType(specialHandler, typeof(SpecialCommandHandlerDecorator<SpecialCommand>));
        }

        [TestMethod]
        public void RegisterDecorator_DecoratorWithGenericTypeConstraint_DoesNotWrapTypesThatNotAdhereToTheConstraint()
        {
            // Arrange
            var container = ContainerFactory.New();

            // RealCommand does not implement ISpecialCommand
            container.Register<ICommandHandler<RealCommand>, NullCommandHandler<RealCommand>>();

            // SpecialCommandHandlerDecorator has a "where T : ISpecialCommand" constraint.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(SpecialCommandHandlerDecorator<>));

            // Act
            var realHandler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(realHandler, typeof(NullCommandHandler<RealCommand>));
        }

        [TestMethod]
        public void GetAllInstances_RegisteringADecoratorThatWrapsTheWholeCollection_WorksAsExpected()
        {
            // Arrange
            var container = new Container();

            container.RegisterAll<ICommandHandler<RealCommand>>(
                typeof(NullCommandHandler<RealCommand>),
                typeof(StubCommandHandler));

            // EnumerableDecorator<T> decorated IEnumerable<T>
            container.RegisterSingleDecorator(
                typeof(IEnumerable<ICommandHandler<RealCommand>>),
                typeof(EnumerableDecorator<ICommandHandler<RealCommand>>));

            // Act
            var collection = container.GetAllInstances<ICommandHandler<RealCommand>>();

            // Assert
            // Wrapping the collection itself instead of the individual elements allows you to apply a filter
            // to the elements, perhaps based on the user's role. I must admit that this is a quite bizarre
            // scenario, but it is currently supported (perhaps even by accident), so we need to have a test
            // to ensure it keeps being supported in the future.
            Assert.IsInstanceOfType(collection, typeof(EnumerableDecorator<ICommandHandler<RealCommand>>));
        }

        [TestMethod]
        public void GetRelationships_AddingRelationshipDuringBuildingOnDecoratorType_ContainsAddedRelationship()
        {
            // Arrange
            var container = ContainerFactory.New();

            var expectedRelationship = GetValidRelationship();

            container.Register<ICommandHandler<RealCommand>, NullCommandHandler<RealCommand>>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RealCommandHandlerDecorator));

            container.ExpressionBuilding += (s, e) =>
            {
                if (e.KnownImplementationType == typeof(RealCommandHandlerDecorator))
                {
                    e.KnownRelationships.Add(expectedRelationship);
                }
            };

            container.Verify();

            // Act
            var relationships =
                container.GetRegistration(typeof(ICommandHandler<RealCommand>)).GetRelationships();

            // Assert
            Assert.IsTrue(relationships.Contains(expectedRelationship),
                "Any known relationships added to the decorator during the ExpressionBuilding event " +
                "should be added to the registration of the service type.");
        }

        // This is a regression test. This test failed on Simple Injector 2.0 to 2.2.3.
        [TestMethod]
        public void RegisterDecorator_AppliedToMultipleInstanceProducersWithTheSameServiceType_CallsThePredicateForEachImplementationType()
        {
            // Arrange
            Type serviceType = typeof(IPlugin);

            var implementationTypes = new List<Type>();

            var container = ContainerFactory.New();

            var prod1 = new InstanceProducer(serviceType,
                Lifestyle.Transient.CreateRegistration(serviceType, typeof(PluginImpl), container));

            var prod2 = new InstanceProducer(serviceType,
                Lifestyle.Transient.CreateRegistration(serviceType, typeof(PluginImpl2), container));

            container.RegisterDecorator(serviceType, typeof(PluginDecorator), context =>
            {
                implementationTypes.Add(context.ImplementationType);
                return true;
            });

            // Act
            var instance1 = prod1.GetInstance();
            var instance2 = prod2.GetInstance();

            // Assert
            string message = "The predicate was expected to be called with a context containing the " +
                "implementation type: ";

            Assert.AreEqual(2, implementationTypes.Count, "Predicate was expected to be called twice.");
            Assert.IsTrue(implementationTypes.Any(type => type == typeof(PluginImpl)),
                message + typeof(PluginImpl).Name);
            Assert.IsTrue(implementationTypes.Any(type => type == typeof(PluginImpl2)),
                message + typeof(PluginImpl2).Name);
        }

        [TestMethod]
        public void Verify_WithProxyDecoratorWrappingAnInvalidRegistration_ShouldFailWithExpressiveException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<ICommandHandler<RealCommand>>(() =>
            {
                throw new Exception("Failure.");
            });

            // AsyncCommandHandlerProxy<T> depends on Func<ICommandHandler<T>>.
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(AsyncCommandHandlerProxy<>));

            // Act
            Action action = () => container.Verify();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<InvalidOperationException>(@"
                The configuration is invalid. 
                Creating the instance for type ICommandHandler<RealCommand> failed.
                Failure."
                .TrimInside(),
                action,
                "Verification should fail because the Func<ICommandHandler<T>> is invalid.");
        }

        [TestMethod]
        public void GetInstance_DecoratorWithNestedGenericType_GetsAppliedCorrectly()
        {
            // Arrange
            var container = new Container();

            container.Register(
                typeof(IQueryHandler<CacheableQuery, ReadOnlyCollection<DayOfWeek>>),
                typeof(CacheableQueryHandler));

            container.Register(
                typeof(IQueryHandler<NonCacheableQuery, DayOfWeek[]>),
                typeof(NonCacheableQueryHandler));

            container.RegisterDecorator(typeof(IQueryHandler<,>), typeof(CacheableQueryHandlerDecorator<,>));

            // Act
            var handler1 = container.GetInstance<IQueryHandler<CacheableQuery, ReadOnlyCollection<DayOfWeek>>>();
            var handler2 = container.GetInstance<IQueryHandler<NonCacheableQuery, DayOfWeek[]>>();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(CacheableQueryHandlerDecorator<CacheableQuery, DayOfWeek>));
            Assert.IsInstanceOfType(handler2, typeof(NonCacheableQueryHandler));
        }

        [TestMethod]
        public void RegisterDecoratorWithFactory_AllValidParameters_Succeeds()
        {
            // Arrange
            var container = new Container();

            var validParameters = RegisterDecoratorFactoryParameters.CreateValid();

            // Act
            container.RegisterDecorator(validParameters);
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithDecoratorReturningOpenGenericType_WrapsTheServiceWithTheClosedDecorator()
        {
            // Arrange
            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<>);
            parameters.DecoratorTypeFactory = context => typeof(TransactionHandlerDecorator<>);

            container.RegisterDecorator(parameters);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithPredicateReturningFalse_DoesNotWrapTheServiceWithTheDecorator()
        {
            // Arrange
            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.Predicate = context => false;
            parameters.ServiceType = typeof(ICommandHandler<>);
            parameters.DecoratorTypeFactory = context => typeof(TransactionHandlerDecorator<>);

            container.RegisterDecorator(parameters);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(RealCommandHandler));
        }

        [TestMethod]
        public void GetInstance_OnDifferentServiceTypeThanRegisteredDecorator_DoesNotCallSuppliedPredicate()
        {
            // Arrange
            bool predicateCalled = false;
            bool decoratorTypeFactoryCalled = false;

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<>);

            parameters.Predicate = context =>
            {
                predicateCalled = true;
                return false;
            };

            parameters.DecoratorTypeFactory = context =>
            {
                decoratorTypeFactoryCalled = true;
                return typeof(TransactionHandlerDecorator<>);
            };

            container.RegisterDecorator(parameters);

            // Act
            // Resolve some other type
            try
            {
                container.GetInstance<INonGenericService>();
            }
            catch
            {
                // This will fail since INonGenericService is not registered.
            }

            // Assert
            Assert.IsFalse(predicateCalled, "The predicate should not be called when a type is resolved " +
                "that doesn't match the given service type (ICommandHandler<TCommand> in this case).");
            Assert.IsFalse(decoratorTypeFactoryCalled, "The factory should not be called.");
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithPredicateReturningFalse_DoesNotCallTheFactory()
        {
            // Arrange
            bool decoratorTypeFactoryCalled = false;

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.Predicate = context => false;
            parameters.ServiceType = typeof(ICommandHandler<>);

            parameters.DecoratorTypeFactory = context =>
            {
                decoratorTypeFactoryCalled = true;
                return typeof(TransactionHandlerDecorator<>);
            };

            container.RegisterDecorator(parameters);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsFalse(decoratorTypeFactoryCalled, @"
                The factory should not be called if the predicate returns false. This prevents the user from 
                having to do specific handling when the decorator type can't be constructed because of generic 
                type constraints.");
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithFactoryReturningTypeBasedOnImplementationType_WrapsTheServiceWithTheExpectedDecorator()
        {
            // Arrange
            var container = new Container();

            container.Register(typeof(INonGenericService), typeof(RealNonGenericService));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(INonGenericService);
            parameters.DecoratorTypeFactory =
                context => typeof(NonGenericServiceDecorator<>).MakeGenericType(context.ImplementationType);

            container.RegisterDecorator(parameters);

            // Act
            var service = container.GetInstance<INonGenericService>();

            // Assert
            Assert.IsInstanceOfType(service, typeof(NonGenericServiceDecorator<RealNonGenericService>));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorReturningAnOpenGenericType_AppliesThatTypeOnlyWhenTypeConstraintsAreMet()
        {
            // Arrange
            var container = new Container();

            // SpecialCommand implements ISpecialCommand, but RealCommand does not.
            container.Register<ICommandHandler<SpecialCommand>, NullCommandHandler<SpecialCommand>>();
            container.Register<ICommandHandler<RealCommand>, NullCommandHandler<RealCommand>>();

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<>);

            // SpecialCommandHandlerDecorator has a "where T : ISpecialCommand" constraint.
            parameters.DecoratorTypeFactory = context => typeof(SpecialCommandHandlerDecorator<>);

            container.RegisterDecorator(parameters);

            // Act
            var handler1 = container.GetInstance<ICommandHandler<SpecialCommand>>();
            var handler2 = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(SpecialCommandHandlerDecorator<SpecialCommand>));
            Assert.IsInstanceOfType(handler2, typeof(NullCommandHandler<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithFactoryReturningAPartialOpenGenericType_WorksLikeACharm()
        {
            // Arrange
            var container = new Container();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<>);

            // Here we make a partial open-generic type by filling in the TUnresolved.
            parameters.DecoratorTypeFactory = context =>
                typeof(CommandHandlerDecoratorWithUnresolvableArgument<,>)
                    .MakePartialOpenGenericType(
                        secondArgument: context.ImplementationType);

            container.RegisterDecorator(parameters);

            // Act
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler,
                typeof(CommandHandlerDecoratorWithUnresolvableArgument<RealCommand, RealCommandHandler>));
        }

        [TestMethod]
        public void GetInstance_WithClosedGenericServiceAndOpenGenericDecoratorReturnedByFactory_ReturnsDecoratedFactory()
        {
            // Arrange
            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);
            parameters.DecoratorTypeFactory = context => typeof(TransactionHandlerDecorator<>);

            container.RegisterDecorator(parameters);

            // Act
            // Registering an closed generic service with an open generic decorator isn't supported by the
            // 'normal' RegisterDecorator methods. This is a limitation in the underlying system. The system
            // can't easily verify whether the open-generic decorator is assignable from the closed-generic
            // service.
            // The factory-supplying version doesn't have this limitation, since the factory is only called
            // at resolve-time, which means there are no open-generic types to check. Everything is closed.
            // So long story short: the following call will (or should) succeed.
            var handler = container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            Assert.IsInstanceOfType(handler, typeof(TransactionHandlerDecorator<RealCommand>));
        }

        [TestMethod]
        public void GetInstance_WithClosedGenericServiceAndFactoryReturningIncompatibleClosedImplementation_FailsWithExpectedException()
        {
            // Arrange
            string expectedMessage = @"
                The registered decorator type factory returned type TransactionHandlerDecorator<Int32> which
                does not implement ICommandHandler<RealCommand>"
                .TrimInside();

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);
            parameters.DecoratorTypeFactory = context => typeof(TransactionHandlerDecorator<int>);

            // Since the creation of the decorator type is delayed, the call to RegisterDecorator can't
            // throw an exception.
            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(
                expectedMessage, action);
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithFactoryReturningTypeWithMultiplePublicConstructors_ThrowsExceptedException()
        {
            // Arrange
            string expectedMessage = "it should contain exactly one public constructor";

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);
            parameters.DecoratorTypeFactory = context => typeof(MultipleConstructorsCommandHandlerDecorator<>);

            // Since the creation of the decorator type is delayed, the call to RegisterDecorator can't
            // throw an exception.
            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(expectedMessage, action);
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithNonGenericServiceAndFactoryReturningAnOpenGenericDecoratorType_ThrowsExpectedException()
        {
            // Arrange
            string expectedMessage = @"The registered decorator type factory returned open generic type 
                NonGenericServiceDecorator<T> while the registered service type INonGenericService is not 
                generic, making it impossible for a closed-generic decorator type to be constructed"
                .TrimInside();

            var container = new Container();

            container.Register(typeof(INonGenericService), typeof(RealNonGenericService));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(INonGenericService);
            parameters.DecoratorTypeFactory = context => typeof(NonGenericServiceDecorator<>);

            // Since the creation of the decorator type is delayed, the call to RegisterDecorator can't
            // throw an exception.
            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<INonGenericService>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(expectedMessage, action);
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithFactoryReturningTypeThatIsNotADecorator_ThrowsExceptedException()
        {
            // Arrange
            string expectedMessage = @"
                For the container to be able to use InvalidDecoratorCommandHandlerDecorator<RealCommand> as  
                a decorator, its constructor must include a single parameter of type 
                ICommandHandler<RealCommand> (or Func<ICommandHandler<RealCommand>>)"
                .TrimInside();

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);
            parameters.DecoratorTypeFactory = context => typeof(InvalidDecoratorCommandHandlerDecorator<>);

            // Since the creation of the decorator type is delayed, the call to RegisterDecorator can't
            // throw an exception.
            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(expectedMessage, action);
        }

        [TestMethod]
        public void GetInstance_RegisterDecoratorWithFactoryReturningTypeWithUnresolvableArgument_ThrowsExceptedException()
        {
            // Arrange
            string expectedMessage =
                typeof(CommandHandlerDecoratorWithUnresolvableArgument<,>).ToFriendlyName() +
                " contains unresolvable type arguments.";

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);

            // CommandHandlerDecoratorWithUnresolvableArgument<T, TUnresolved> contains an unmappable 
            // type argument TUnresolved.
            parameters.DecoratorTypeFactory =
                context => typeof(CommandHandlerDecoratorWithUnresolvableArgument<,>);

            // Since the creation of the decorator type is delayed, the call to RegisterDecorator can't
            // throw an exception.
            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(expectedMessage, action);
        }

        [TestMethod]
        public void RegisterDecoratorWithFactory_InvalidDecoratorTypeFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var container = new Container();

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.DecoratorTypeFactory = null;

            // Act
            Action action = () => container.RegisterDecorator(parameters);

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("decoratorTypeFactory", action);
        }

        [TestMethod]
        public void RegisterDecoratorWithFactory_FactoryThatReturnsNull_ThrowsExpectedExceptionWhenResolving()
        {
            // Arrange
            string expectedExceptionMessage =
                "The decorator type factory delegate that was registered for service type " +
                "ICommandHandler<RealCommand> returned null.";

            var container = new Container();

            container.Register(typeof(ICommandHandler<RealCommand>), typeof(RealCommandHandler));

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.DecoratorTypeFactory = context => null;

            parameters.ServiceType = typeof(ICommandHandler<RealCommand>);

            container.RegisterDecorator(parameters);

            // Act
            Action action = () => container.GetInstance<ICommandHandler<RealCommand>>();

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<ActivationException>(expectedExceptionMessage, action);
        }

        [TestMethod]
        public void RegisterDecoratorWithFactory_InvalidPredicate_ThrowsArgumentNullException()
        {
            // Arrange
            var container = new Container();

            var parameters = RegisterDecoratorFactoryParameters.CreateValid();

            parameters.Predicate = null;

            // Act
            Action action = () => container.RegisterDecorator(parameters);

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("predicate", action);
        }

        [TestMethod]
        public void GetInstance_DecoratorDependingOnDecoratorPredicateContext_ContainsTheExpectedContext()
        {
            // Arrange
            var container = new Container();

            container.Register<ICommandHandler<RealCommand>, RealCommandHandler>();

            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TransactionHandlerDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(ContextualHandlerDecorator<>));

            // Act
            var decorator = 
                (ContextualHandlerDecorator<RealCommand>)container.GetInstance<ICommandHandler<RealCommand>>();

            DecoratorContext context = decorator.Context;

            // Assert
            Assert.AreSame(typeof(RealCommandHandler), context.ImplementationType);
            Assert.AreSame(typeof(TransactionHandlerDecorator<RealCommand>), context.AppliedDecorators.Single());
        }

        private static KnownRelationship GetValidRelationship()
        {
            // Arrange
            var container = new Container();

            return new KnownRelationship(typeof(object), Lifestyle.Transient,
                container.GetRegistration(typeof(Container)));
        }
    }

    public static class ContainerTestExtensions
    {
        internal static void RegisterDecorator(this Container container,
            RegisterDecoratorFactoryParameters parameters)
        {
            container.RegisterDecorator(parameters.ServiceType, parameters.DecoratorTypeFactory,
                parameters.Lifestyle, parameters.Predicate);
        }
    }

    public class RegisterDecoratorFactoryParameters
    {
        public static RegisterDecoratorFactoryParameters CreateValid()
        {
            return new RegisterDecoratorFactoryParameters
            {
                ServiceType = typeof(ICommandHandler<>),
                DecoratorTypeFactory = context => typeof(AsyncCommandHandlerProxy<>),
                Lifestyle = Lifestyle.Transient,
                Predicate = context => true,
            };
        }

        public Type ServiceType { get; set; }

        public Func<DecoratorPredicateContext, Type> DecoratorTypeFactory { get; set; }

        public Lifestyle Lifestyle { get; set; }

        public Predicate<DecoratorPredicateContext> Predicate { get; set; }
    }

    public class DependencyInfo
    {
        public DependencyInfo(Type serviceType, Lifestyle lifestyle)
        {
            this.ServiceType = serviceType;
            this.Lifestyle = lifestyle;
        }

        public Type ServiceType { get; private set; }

        public Lifestyle Lifestyle { get; private set; }
    }

    public sealed class FakeLogger : ILogger
    {
        public string Message { get; private set; }

        public void Log(string message)
        {
            this.Message += message;
        }
    }

    public class RealCommand
    {
    }

    public class SpecialCommand : ISpecialCommand
    {
    }

    public class MultipleConstructorsCommandHandlerDecorator<T> : ICommandHandler<T>
    {
        public MultipleConstructorsCommandHandlerDecorator()
        {
        }

        public MultipleConstructorsCommandHandlerDecorator(ICommandHandler<T> decorated)
        {
        }

        public void Handle(T command)
        {
        }
    }

    public class InvalidDecoratorCommandHandlerDecorator<T> : ICommandHandler<T>
    {
        // This is no decorator, since it lacks the ICommandHandler<T> parameter.
        public InvalidDecoratorCommandHandlerDecorator(ILogger logger)
        {
        }

        public void Handle(T command)
        {
        }
    }

    public class InvalidDecoratorCommandHandlerDecorator2<T> : ICommandHandler<T>
    {
        // This is not a decorator as it expects more than one ICommandHandler<T> parameter.
        public InvalidDecoratorCommandHandlerDecorator2(ICommandHandler<T> decorated1,
            ICommandHandler<T> decorated2, ILogger logger)
        {
        }

        public void Handle(T command)
        {
        }
    }

    public class NullCommandHandler<T> : ICommandHandler<T>
    {
        public void Handle(T command)
        {
        }
    }

    public class StubCommandHandler : ICommandHandler<RealCommand>
    {
        public virtual void Handle(RealCommand command)
        {
        }
    }

    public class StructCommandHandler : ICommandHandler<StructCommand>
    {
        public void Handle(StructCommand command)
        {
        }
    }

    public class RealCommandHandler : ICommandHandler<RealCommand>
    {
        public void Handle(RealCommand command)
        {
        }
    }

    public class LoggingRealCommandHandler : ICommandHandler<RealCommand>
    {
        private readonly ILogger logger;

        public LoggingRealCommandHandler(ILogger logger)
        {
            this.logger = logger;
        }

        public void Handle(RealCommand command)
        {
            this.logger.Log("RealCommand");
        }
    }

    public class RealCommandHandlerDecorator : ICommandHandler<RealCommand>
    {
        public RealCommandHandlerDecorator(ICommandHandler<RealCommand> decorated)
        {
            this.Decorated = decorated;
        }

        public ICommandHandler<RealCommand> Decorated { get; private set; }

        public void Handle(RealCommand command)
        {
        }
    }

    public class TransactionHandlerDecorator<T> : ICommandHandler<T>
    {
        public TransactionHandlerDecorator(ICommandHandler<T> decorated)
        {
            this.Decorated = decorated;
        }

        public ICommandHandler<T> Decorated { get; private set; }

        public void Handle(T command)
        {
        }
    }

    public class ContextualHandlerDecorator<T> : ICommandHandler<T>
    {
        public ContextualHandlerDecorator(ICommandHandler<T> decorated, DecoratorContext context)
        {
            this.Decorated = decorated;
            this.Context = context;
        }

        public ICommandHandler<T> Decorated { get; private set; }

        public DecoratorContext Context { get; private set; }

        public void Handle(T command)
        {
        }
    }

    public class SpecialCommandHandlerDecorator<T> : ICommandHandler<T> where T : ISpecialCommand
    {
        public SpecialCommandHandlerDecorator(ICommandHandler<T> decorated)
        {
        }

        public void Handle(T command)
        {
        }
    }

    public class LogExceptionCommandHandlerDecorator<T> : ICommandHandler<T>
    {
        private readonly ICommandHandler<T> decorated;

        public LogExceptionCommandHandlerDecorator(ICommandHandler<T> decorated)
        {
            this.decorated = decorated;
        }

        public void Handle(T command)
        {
            // called the decorated instance and log any exceptions (not important for these tests).
        }
    }

    public class LoggingHandlerDecorator1<T> : ICommandHandler<T>
    {
        private readonly ICommandHandler<T> wrapped;
        private readonly ILogger logger;

        public LoggingHandlerDecorator1(ICommandHandler<T> wrapped, ILogger logger)
        {
            this.wrapped = wrapped;
            this.logger = logger;
        }

        public void Handle(T command)
        {
            this.logger.Log("Begin1 ");
            this.wrapped.Handle(command);
            this.logger.Log(" End1");
        }
    }

    public class LoggingHandlerDecorator2<T> : ICommandHandler<T>
    {
        private readonly ICommandHandler<T> wrapped;
        private readonly ILogger logger;

        public LoggingHandlerDecorator2(ICommandHandler<T> wrapped, ILogger logger)
        {
            this.wrapped = wrapped;
            this.logger = logger;
        }

        public void Handle(T command)
        {
            this.logger.Log("Begin2 ");
            this.wrapped.Handle(command);
            this.logger.Log(" End2");
        }
    }

    public class AsyncCommandHandlerProxy<T> : ICommandHandler<T>
    {
        public AsyncCommandHandlerProxy(Container container, Func<ICommandHandler<T>> decorateeFactory)
        {
            this.DecorateeFactory = decorateeFactory;
        }

        public Func<ICommandHandler<T>> DecorateeFactory { get; private set; }

        public void Handle(T command)
        {
            // Run decorated instance on new thread (not important for these tests).
        }
    }

    public class ClassConstraintHandlerDecorator<T> : ICommandHandler<T> where T : class
    {
        public ClassConstraintHandlerDecorator(ICommandHandler<T> wrapped)
        {
        }

        public void Handle(T command)
        {
        }
    }

    // This is not a decorator, the class implements ICommandHandler<int> but wraps ICommandHandler<byte>
    public class BadCommandHandlerDecorator1 : ICommandHandler<int>
    {
        public BadCommandHandlerDecorator1(ICommandHandler<byte> handler)
        {
        }

        public void Handle(int command)
        {
        }
    }

    // This is not a decorator, the class takes 2 generic types but wraps ICommandHandler<T>
    public class CommandHandlerDecoratorWithUnresolvableArgument<T, TUnresolved> : ICommandHandler<T>
    {
        public CommandHandlerDecoratorWithUnresolvableArgument(ICommandHandler<T> handler)
        {
        }

        public void Handle(T command)
        {
        }
    }

    public class HandlerDecoratorWithPropertiesBase
    {
        public int Item1 { get; set; }

        public string Item2 { get; set; }
    }

    public class HandlerDecoratorWithProperties<T> : HandlerDecoratorWithPropertiesBase, ICommandHandler<T>
    {
        private readonly ICommandHandler<T> wrapped;

        public HandlerDecoratorWithProperties(ICommandHandler<T> wrapped)
        {
            this.wrapped = wrapped;
        }

        public void Handle(T command)
        {
        }
    }

    public class RealNonGenericService : INonGenericService
    {
        public void DoSomething()
        {
        }
    }

    public class NonGenericServiceDecorator : INonGenericService
    {
        public NonGenericServiceDecorator(INonGenericService decorated)
        {
            this.DecoratedService = decorated;
        }

        public INonGenericService DecoratedService { get; private set; }

        public void DoSomething()
        {
            this.DecoratedService.DoSomething();
        }
    }

    public class NonGenericServiceDecorator<T> : INonGenericService
    {
        public NonGenericServiceDecorator(INonGenericService decorated)
        {
            this.DecoratedService = decorated;
        }

        public INonGenericService DecoratedService { get; private set; }

        public void DoSomething()
        {
            this.DecoratedService.DoSomething();
        }
    }

    public class NonGenericServiceDecoratorWithFunc : INonGenericService
    {
        public NonGenericServiceDecoratorWithFunc(Func<INonGenericService> decoratedCreator)
        {
            this.DecoratedServiceCreator = decoratedCreator;
        }

        public Func<INonGenericService> DecoratedServiceCreator { get; private set; }

        public void DoSomething()
        {
            this.DecoratedServiceCreator().DoSomething();
        }
    }

    public class NonGenericServiceDecoratorWithBothDecorateeAndFunc : INonGenericService
    {
        public NonGenericServiceDecoratorWithBothDecorateeAndFunc(INonGenericService decoratee,
            Func<INonGenericService> decoratedCreator)
        {
        }

        public void DoSomething()
        {
        }
    }

    public class EnumerableDecorator<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> decoratedCollection;

        public EnumerableDecorator(IEnumerable<T> decoratedCollection)
        {
            this.decoratedCollection = decoratedCollection;
        }

        public IEnumerator<T> GetEnumerator()
        {
            // Scenario: do some filtering here, based on the user's role.
            return this.decoratedCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public static class TypeExtensions
    {
        public static Type MakePartialOpenGenericType(this Type type, Type firstArgument = null,
            Type secondArgument = null)
        {
            var arguments = type.GetGenericArguments();

            if (firstArgument != null)
            {
                arguments[0] = firstArgument;
            }

            if (secondArgument != null)
            {
                arguments[1] = secondArgument;
            }

            return type.MakeGenericType(arguments);
        }
    }

    internal class RelationshipInfo
    {
        public Type ImplementationType { get; set; }

        public Lifestyle Lifestyle { get; set; }

        public DependencyInfo Dependency { get; set; }

        internal static bool EqualsTo(RelationshipInfo info, KnownRelationship other)
        {
            return
                info.ImplementationType == other.ImplementationType &&
                info.Lifestyle == other.Lifestyle &&
                info.Dependency.ServiceType == other.Dependency.ServiceType &&
                info.Dependency.Lifestyle == other.Dependency.Lifestyle;
        }

        internal bool Equals(KnownRelationship other)
        {
            return EqualsTo(this, other);
        }

        internal static string ToString(KnownRelationship relationship)
        {
            return string.Format("ImplementationType: {0}, Lifestyle: {1}, Dependency: {2}",
                relationship.ImplementationType.ToFriendlyName(),
                relationship.Lifestyle.Name,
                string.Format("{{ ServiceType: {0}, Lifestyle: {1} }}",
                    relationship.Dependency.ServiceType.ToFriendlyName(),
                    relationship.Dependency.Lifestyle.Name));
        }
    }
}