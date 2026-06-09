# نمودار جریان داده (DFD) برای فرآیند سفارش تا تحویل

## سطح 0 (Context Diagram):
- External Entities: Customer, Warehouse, Payment Gateway, Delivery Service
- Process: Order Management System
- Data Flows: Order Request, Confirmation, Payment, Delivery Info

## سطح 1:

1. **Process 1.0: ثبت سفارش**
   - Input: Customer Info, Product Selection
   - Output: Order Record
   - Data Store: Orders DB

2. **Process 2.0: اعتبارسنجی و تأیید**
   - Check stock, customer validity
   - Output: Approved Order

3. **Process 3.0: پردازش پرداخت**
   - Input: Payment Details
   - Output: Payment Confirmation
   - External: Payment Gateway

4. **Process 4.0: آماده‌سازی و بسته‌بندی**
   - Update Inventory
   - Data Store: Inventory DB

5. **Process 5.0: تحویل**
   - Output: Delivery Status to Customer

**داده‌ها:**
- Data Stores: Customers, Products, Orders, Inventory
- Flows: بین فرآیندها و ذخیره‌گاه‌ها

برای drawio: از اشکال DFD استاندارد (Process: rounded rectangle, Data Store: open rectangle, External Entity: square, Flow: arrows) استفاده کنید.