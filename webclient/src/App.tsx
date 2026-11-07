import {
    Button,
    Col,
    Descriptions,
    Divider,
    Image,
    Layout,
    List,
    Modal,
    message,
    Row,
    Space,
    Table,
    Tag,
    Typography,
    Upload,
    type TableColumnsType,
} from "antd";
import axios from "axios";
import { useCallback, useEffect, useState } from "react";

enum ReceiptStatus {
    Created = 0,
    TextExctracted,
    Finished,
}

const receiptStatusTag: Record<number, { title: string; color: string }> = {
    [ReceiptStatus.Created]: { title: "Создан", color: "blue" },
    [ReceiptStatus.TextExctracted]: { title: "Текст извлечён", color: "orange" },
    [ReceiptStatus.Finished]: { title: "Обработан", color: "green" },
};

interface Receipt {
    id: number;
    totalSum?: number;
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

interface ReceiptHistoryItem {
    status: ReceiptStatus;
    atRecord: string;
}

interface ReceiptDetails extends Receipt {
    rawText?: string;
    history: ReceiptHistoryItem[];
}

const statusTag = (status: ReceiptStatus) => {
    const tag = receiptStatusTag[status] ?? { title: "Неизвестен", color: "default" };
    return <Tag color={tag.color}>{tag.title}</Tag>;
};

const columns: TableColumnsType<Receipt> = [
    { title: "№", dataIndex: "id", key: "id" },
    {
        title: "Сумма",
        key: "totalSum",
        render: (_, receipt) => `${receipt.totalSum ?? "—"} ${receipt.currency ?? ""}`,
    },
    { title: "Статус", dataIndex: "status", key: "status", render: statusTag },
];

function App() {
    const [receipts, setReceipts] = useState<Receipt[]>([]);
    const [selectedReceipt, setSelectedReceipt] = useState<ReceiptDetails | null>(null);
    const [isDetailsLoading, setIsDetailsLoading] = useState(false);
    const [messageApi, contextHolder] = message.useMessage();

    const fetchReceipts = useCallback(async () => {
        const response = await axios.get<Receipt[]>("/api/receipts");
        setReceipts(response.data);
    }, []);

    useEffect(() => {
        const load = async () => {
            try {
                await fetchReceipts();
            } catch {
                messageApi.error("Не удалось загрузить список чеков");
            }
        };
        void load();

        const timer = window.setInterval(() => {
            void fetchReceipts().catch(() => undefined);
        }, 5000);

        return () => window.clearInterval(timer);
    }, [fetchReceipts, messageApi]);

    const openReceipt = async (receipt: Receipt) => {
        setIsDetailsLoading(true);
        try {
            const response = await axios.get<ReceiptDetails>(`/api/receipts/${receipt.id}`);
            setSelectedReceipt(response.data);
        } catch {
            messageApi.error("Не удалось загрузить детализацию чека");
        } finally {
            setIsDetailsLoading(false);
        }
    };

    const uploadReceipt = async (file: File) => {
        const formData = new FormData();
        formData.append("file", file);
        try {
            await axios.post("/api/receipts/upload", formData);
            messageApi.success("Чек загружен и отправлен на обработку");
            await fetchReceipts();
        } catch {
            messageApi.error("Не удалось загрузить чек");
        }
    };

    return (
        <Layout>
            {contextHolder}
            <Row gutter={[8, 8]} style={{ margin: 5 }}>
                <Col span={24}>
                    <Typography.Title style={{ textAlign: "center" }}>Чеки</Typography.Title>
                    <Divider />
                </Col>
                <Col span={24}>
                    <Upload
                        accept="image/*"
                        maxCount={1}
                        showUploadList={false}
                        beforeUpload={(file) => {
                            void uploadReceipt(file);
                            return false;
                        }}
                    >
                        <Button>Загрузить чек</Button>
                    </Upload>
                </Col>
                <Col span={24}>
                    <Table<Receipt>
                        rowKey="id"
                        dataSource={receipts}
                        columns={columns}
                        loading={isDetailsLoading}
                        onRow={(receipt) => ({
                            onClick: () => void openReceipt(receipt),
                            style: { cursor: "pointer" },
                        })}
                    />
                </Col>
            </Row>

            <Modal
                title={selectedReceipt ? `Чек №${selectedReceipt.id}` : "Детализация чека"}
                open={selectedReceipt !== null}
                onCancel={() => setSelectedReceipt(null)}
                footer={null}
                width={900}
            >
                {selectedReceipt && (
                    <Space direction="vertical" size="large" style={{ width: "100%" }}>
                        <Descriptions bordered column={{ xs: 1, sm: 2 }}>
                            <Descriptions.Item label="Статус">{statusTag(selectedReceipt.status)}</Descriptions.Item>
                            <Descriptions.Item label="Итого">
                                {selectedReceipt.totalSum ?? "—"} {selectedReceipt.currency}
                            </Descriptions.Item>
                        </Descriptions>

                        <Image
                            src={`/api/receipts/${selectedReceipt.id}/image`}
                            alt={`Изображение чека №${selectedReceipt.id}`}
                            style={{ maxHeight: 400, maxWidth: "100%", objectFit: "contain" }}
                        />

                        <Typography.Title level={5}>Позиции</Typography.Title>
                        <Table<ReceiptItem>
                            size="small"
                            pagination={false}
                            rowKey="id"
                            dataSource={selectedReceipt.items}
                            columns={[
                                { title: "Товар", dataIndex: "name", key: "name" },
                                { title: "Количество", dataIndex: "quantity", key: "quantity" },
                                { title: "Цена", dataIndex: "price", key: "price" },
                                { title: "Сумма", dataIndex: "sum", key: "sum" },
                            ]}
                        />

                        <Typography.Title level={5}>Сырой текст OCR</Typography.Title>
                        <Typography.Paragraph copyable={{ text: selectedReceipt.rawText ?? "" }}>
                            <pre style={{ whiteSpace: "pre-wrap", margin: 0 }}>{selectedReceipt.rawText || "Текст ещё не извлечён"}</pre>
                        </Typography.Paragraph>

                        <Typography.Title level={5}>История статусов</Typography.Title>
                        <List
                            size="small"
                            bordered
                            dataSource={selectedReceipt.history}
                            locale={{ emptyText: "История отсутствует" }}
                            renderItem={(item) => (
                                <List.Item>
                                    <span>{statusTag(item.status)}</span>
                                    <Typography.Text type="secondary">
                                        {new Date(item.atRecord).toLocaleString("ru-RU")}
                                    </Typography.Text>
                                </List.Item>
                            )}
                        />
                    </Space>
                )}
            </Modal>
        </Layout>
    );
}

export default App;
