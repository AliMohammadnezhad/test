# مدل ERD برای سیستم فروشگاه

## موجودیت‌ها و ویژگی‌ها:

### 1. Customer (مشتری)
- customer_id (PK)
- name
- email
- phone
- address
- registration_date

### 2. Product (کالا)
- product_id (PK)
- name
- category (لوازم تحریر، بهداشتی، etc.)
- price
- stock_quantity
- description

### 3. Order (سفارش)
- order_id (PK)
- customer_id (FK)
- order_date
- total_amount
- status (pending, confirmed, shipped, delivered)
- payment_method

### 4. OrderItem (آیتم سفارش)
- order_item_id (PK)
- order_id (FK)
- product_id (FK)
- quantity
- unit_price

### 5. AuditLog (سوابق ممیزی)
- log_id (PK)
- entity_type (e.g., Order)
- entity_id
- action (create, update, delete)
- changed_by (user_id)
- change_date
- old_value
- new_value

## روابط:
- Customer 1:N Order
- Order 1:N OrderItem
- Product 1:N OrderItem
- Order N:1 AuditLog (or via triggers)

**قیود یکپارچگی:**
- Foreign Key constraints
- NOT NULL on key fields
- CHECK constraints on status and quantities
- Triggers for audit logging on changes

برای ایجاد دیاگرام drawio، از موجودیت‌ها بالا استفاده کنید و روابط را با خطوط و cardinality نمایش دهید.