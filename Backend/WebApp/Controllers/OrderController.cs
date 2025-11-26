using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Domain.Entities;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
    readonly IOrderService  _orderService;
    readonly IPaymentService _paymentService;
    public OrderController(IOrderService orderService,IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }
    
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckOut() // make order and handel payment 
    {

          var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); 
          OrderSummaryDto order;
            // 2. Parse it to a Guid (since your service expects a Guid)
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                // 3. Call your service
                 order = await _orderService.CreateOrderAsync(userId);
            }
            else
            {
                // Handle the case where ID is null or invalid (e.g., return Unauthorized)
                return Unauthorized("User ID not found in token.");
            }
    

        var aThing=await _paymentService.CreateOrUpdatePaymentIntent(order.OrderId);

        return Ok(new {theThing=aThing , Message="all done bro"});
    }

    [HttpPost("Cancel")]
    public async Task<IActionResult> CancelOrder(int OrderId)
    {
        try
        {
            await _orderService.CancelOrderAsync(OrderId);
        }
        catch(Exception e)
        {
            throw e; 
        }

        return Ok("IDK , but every thing seem alright ");
    }
    
    
    
    
    
    
}