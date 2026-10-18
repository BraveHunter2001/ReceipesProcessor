import { Col, Divider, Layout, Row, Table, Tag, Typography, type TableColumnsType } from "antd";
import axios from "axios";
import { useEffect, useState } from "react";

const receiptColumn: TableColumnsType<Receipt> = [
    {
        title: "id",
        dataIndex: "id",
        key: "id",
    },
    {
        title: "totalSum",
        dataIndex: "totalSum",
        key: "totalSum",
    },
    {
        title: "currency",
        dataIndex: "currency",
        key: "currency",
    },
    {
        title: "status",
        dataIndex: "status",
        key: "status",
        render: (s: ReceiptStatus) => {
            const tag = ReceiptStatusTag[s];
            return (
                <Tag color={tag.color} key={tag.title}>
                    {tag.title}
                </Tag>
            );
        },
    },
    {
        title: "ownerKey",
        dataIndex: "ownerKey",
        key: "ownerKey",
    },
];

const expandColumns: TableColumnsType<ReceiptItem> = [
    { title: "id", dataIndex: "id", key: "id" },
    { title: "name", dataIndex: "name", key: "name" },
    { title: "quantity", dataIndex: "quantity", key: "quantity" },
    { title: "price", dataIndex: "price", key: "price" },
    { title: "sum", dataIndex: "sum", key: "sum" },
];

enum ReceiptStatus {
    Created = 0,
    TextExctracted,
    Finished,
}

const ReceiptStatusTag = {
    [ReceiptStatus.Created]: { title: "Created", color: "blue" },
    [ReceiptStatus.TextExctracted]: { title: "TextExctracted", color: "orange" },
    [ReceiptStatus.Finished]: { title: "Finished", color: "green" },
};

interface Receipt {
    id: number;
    totalSum: number;
    currency: string;
    status: ReceiptStatus;
    ownerKey: string;
    items: ReceiptItem[];
}

interface ReceiptItem {
    id: number;
    name: string;
    quantity: number;
    price: number;
    sum: number;
}
const expandedRowRender = (r: Receipt) => <Table<ReceiptItem> size="small" columns={expandColumns} dataSource={r.items} pagination={false} rowKey="id" />;

function App() {
    const [receipts, setReceipts] = useState<Receipt[]>([]);
    useEffect(() => {
        // Используем async/await внутри useEffect через самовызывающуюся функцию
        const fetchData = async () => {
            try {
                const response = await axios.get("api/receipts");
                setReceipts(response.data); // Сохраняем данные в state
            } catch (err) {
            } finally {
            }
        };

        fetchData();
    }, []);

    return (
        <Layout>
            <Row gutter={[8, 8]} style={{ margin: 5 }}>
                <Col span={24}>
                    <Typography.Title style={{ textAlign: "center" }}>Receipts</Typography.Title>
                    <Divider />
                </Col>
                <Col span={24}>
                    <Table<Receipt>
                        rowKey="id"
                        dataSource={receipts}
                        columns={receiptColumn}
                        expandable={{ expandedRowRender, defaultExpandedRowKeys: ["0"] }}
                    />
                </Col>
            </Row>
        </Layout>
    );
}

export default App;
