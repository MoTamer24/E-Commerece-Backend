using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class MockPaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(IConfiguration config, IUnitOfWork unitOfWork, ILogger<MockPaymentService> logger)
        {
            _config = config;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> CreateOrUpdatePaymentIntent(int orderId)
        {
            _logger.LogInformation("Creating or updating payment intent for Order {OrderId}", orderId);

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Payment intent processing failed: Order {OrderId} not found", orderId);
                throw new KeyNotFoundException("Order not found");
            }

            await _unitOfWork.SaveAllChangesAsync();

            var intentId = Guid.NewGuid().ToString();
            _logger.LogInformation("Successfully generated payment intent {IntentId} for Order {OrderId}", intentId, orderId);
            return intentId;
        }

        public async Task<PaymentDto> GetPaymentDetailsForOrderAsync(int orderId)
        {
            _logger.LogInformation("Fetching payment details for Order {OrderId}", orderId);

            var payment = await _unitOfWork.Payments.FindAsync(p => p.OrderId == orderId);
            if (payment == null)
            {
                _logger.LogInformation("No payment details found for Order {OrderId}", orderId);
                return null;
            }

            _logger.LogInformation("Retrieved payment details for Order {OrderId} with status {PaymentStatus}", orderId, payment.Status);
            return new PaymentDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Status = payment.Status
            };
        }
    }
}