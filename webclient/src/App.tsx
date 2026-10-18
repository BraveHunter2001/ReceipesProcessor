import { Layout, Typography } from "antd";
import axios from "axios";
import { useEffect, useState } from "react";

function App() {
    const [text, setText] = useState<String>("none");
    useEffect(() => {
        // Используем async/await внутри useEffect через самовызывающуюся функцию
        const fetchData = async () => {
            try {
                const response = await axios.get("api/test");
                setText(response.data); // Сохраняем данные в state
            } catch (err) {
                setText(err.message); // Сохраняем ошибку, если она произошла
            } finally {
            }
        };

        fetchData();
    }, []); // Пустой массив зависимостей означает, что запрос отработает 1 раз при монтировании

    return (
        <Layout>
            <Typography.Text>{text}</Typography.Text>
        </Layout>
    );
}

export default App;
