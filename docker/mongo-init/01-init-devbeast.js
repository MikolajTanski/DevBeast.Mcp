db = db.getSiblingDB('devbeast');

db.createUser({
  user: 'devbeast_app',
  pwd: 'devbeast_app',
  roles: [{ role: 'readWrite', db: 'devbeast' }]
});

db.products.insertMany([
  { _id: ObjectId(), sku: 'SKU-001', name: 'Laptop Pro 15', category: 'Electronics', price: 4999.99, stock: 42, createdAt: new Date('2025-01-15') },
  { _id: ObjectId(), sku: 'SKU-002', name: 'Wireless Mouse', category: 'Accessories', price: 129.99, stock: 200, createdAt: new Date('2025-02-01') },
  { _id: ObjectId(), sku: 'SKU-003', name: 'USB-C Hub', category: 'Accessories', price: 249.00, stock: 85, createdAt: new Date('2025-03-10') }
]);

db.customers.insertMany([
  { _id: ObjectId(), email: 'jan.kowalski@example.com', firstName: 'Jan', lastName: 'Kowalski', tier: 'Gold', registeredAt: new Date('2024-06-01') },
  { _id: ObjectId(), email: 'anna.nowak@example.com', firstName: 'Anna', lastName: 'Nowak', tier: 'Silver', registeredAt: new Date('2024-09-12') }
]);

db.orders.insertMany([
  {
    _id: ObjectId(),
    orderNumber: 'ORD-2025-001',
    customerEmail: 'jan.kowalski@example.com',
    status: 'Completed',
    totalAmount: 5129.98,
    items: [
      { sku: 'SKU-001', quantity: 1, unitPrice: 4999.99 },
      { sku: 'SKU-002', quantity: 1, unitPrice: 129.99 }
    ],
    createdAt: new Date('2025-07-01T10:30:00Z')
  },
  {
    _id: ObjectId(),
    orderNumber: 'ORD-2025-002',
    customerEmail: 'anna.nowak@example.com',
    status: 'Pending',
    totalAmount: 249.00,
    items: [{ sku: 'SKU-003', quantity: 1, unitPrice: 249.00 }],
    createdAt: new Date('2025-07-05T14:15:00Z')
  }
]);

db.deadLetterMessages.insertMany([
  {
    _id: ObjectId(),
    messageId: 'msg-001',
    queue: 'orders.processing',
    reason: 'InvalidOrderState',
    payload: { orderNumber: 'ORD-2025-002', action: 'ProcessPayment' },
    error: 'Order is in Pending state, expected Confirmed',
    failedAt: new Date('2025-07-05T14:16:00Z'),
    retryCount: 3
  },
  {
    _id: ObjectId(),
    messageId: 'msg-002',
    queue: 'inventory.sync',
    reason: 'DeserializationError',
    payload: { sku: 'SKU-999', delta: 'not-a-number' },
    error: 'JSON deserialization failed for field delta',
    failedAt: new Date('2025-07-06T09:00:00Z'),
    retryCount: 5
  }
]);

print('DevBeast sample MongoDB database initialized.');
