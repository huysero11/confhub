namespace ConfHub.Scheduling.Constraints;

// Một vi phạm ràng buộc cứng. Message chỉ để log và test.
/*
ConstraintViolation
├── Code = H1
├── SessionIds = [1, 2]
└── Message = "Phiên 1 và 2 chồng giờ..."
*/
public sealed record ConstraintViolation(ConstraintCode Code, IReadOnlyList<int> SessionIds, string Message);
