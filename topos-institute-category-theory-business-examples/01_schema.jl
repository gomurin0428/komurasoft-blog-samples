using Catlab

# 受注・顧客・商品のスキーマ。Ob が表、Hom が外部キー、Attr が列
@present SchOrders(FreeSchema) begin
  (Customer, Product, Order)::Ob
  customer::Hom(Order, Customer)
  product::Hom(Order, Product)

  (Name, Count, Yen)::AttrType
  customer_name::Attr(Customer, Name)
  product_name::Attr(Product, Name)
  price::Attr(Product, Yen)
  quantity::Attr(Order, Count)
end

@acset_type Orders(SchOrders)

db = @acset Orders{String, Int, Int} begin
  Customer = 2
  customer_name = ["A社", "B社"]
  Product = 2
  product_name = ["ねじ", "ナット"]
  price = [12, 8]
  Order = 3
  customer = [1, 2, 1]
  product = [1, 1, 2]
  quantity = [100, 50, 200]
end

# 外部キーを辿る: 受注 → 顧客の名前、受注 → 商品の単価
for o in parts(db, :Order)
  name = db[o, (:customer, :customer_name)]
  item = db[o, (:product, :product_name)]
  total = db[o, :quantity] * db[o, (:product, :price)]
  println("受注$o: $name が $item を $(db[o, :quantity]) 個、$total 円")
end

# 逆向き: A社（顧客1）の受注を探す
println("A社の受注: ", incident(db, 1, :customer))
