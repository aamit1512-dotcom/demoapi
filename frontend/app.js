
const API_BASE_URL = "http://localhost:90";

const form = document.getElementById("weatherForm");
const message = document.getElementById("message");
const results = document.getElementById("results");

form.addEventListener("submit", async (event) => {
    event.preventDefault();

    const city = document.getElementById("city").value.trim();
    const days = document.getElementById("days").value;

    message.textContent = "Loading forecast...";
    results.replaceChildren();

    try {
        const url =
            `${API_BASE_URL}/api/weatherforecast` +
            `?city=${encodeURIComponent(city)}&days=${days}`;

        const response = await fetch(url);

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || `HTTP ${response.status}`);
        }

        const data = await response.json();

        message.textContent =
            `Forecast for ${data.city} — ${data.days} days`;

        data.forecast.forEach((item) => {
            const card = document.createElement("article");
            card.className = "card";

            const title = document.createElement("h3");
            title.textContent = item.date;

            const high = document.createElement("p");
            high.textContent = `Maximum: ${item.temperatureMaxC} °C`;

            const low = document.createElement("p");
            low.textContent = `Minimum: ${item.temperatureMinC} °C`;

            const rain = document.createElement("p");
            rain.textContent =
                `Precipitation probability: ${
                    item.precipitationProbabilityPercent ?? "N/A"
                }%`;

            card.append(title, high, low, rain);
            results.appendChild(card);
        });
    } catch (error) {
        message.textContent =
            `Unable to load weather: ${error.message}`;
    }
});
