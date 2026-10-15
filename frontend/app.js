
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
        const responseText = await response.text();

        let data = null;

        if (responseText.trim()) {
            try {
                data = JSON.parse(responseText);
            } catch {
                throw new Error(
                    `Invalid response from API (HTTP ${response.status}).`
                );
            }
        }

        if (!response.ok) {
            throw new Error(
                data?.error || `API request failed: HTTP ${response.status}`
            );
        }

        if (!data || !Array.isArray(data.forecast)) {
            throw new Error("The API returned no forecast data.");
        }

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
