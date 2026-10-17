using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Domain.Exceptions
{
    public class DomainExceptions
    {
        public abstract class DomainException(string message) : Exception(message);

        public class DomainValidationException(string message) : DomainException(message); // 400
        public class NotFoundException(string message) : DomainException(message); // 404
        public class ConflictException(string message) : DomainException(message); // 409

        public class UnauthorizedException(string message) : DomainException(message); // 401

        public sealed class InvalidStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
            : DomainValidationException($"Cannot change status from {from} to {to}. Status can only move forward: Pending → InProgress → Done.");



    }

}
